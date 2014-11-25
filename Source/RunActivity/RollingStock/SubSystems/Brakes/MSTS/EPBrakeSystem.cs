// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

namespace ORTS
{

    public class EPBrakeSystem : AirSinglePipe
    {
        ValveState epState = ValveState.Lap;

        public EPBrakeSystem(TrainCar car)
            : base(car)
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            if (BrakeLine1PressurePSI < 0)
            {
                // Brake lines are disconnected.
                TripleValveState = ValveState.Release;
                Car.BrakeForceN = 0;
                return;
            }

            ValveState prevState = epState;
            RetainerPressureThresholdPSI = Car.Train.BrakeLine4PressurePSI;
            if (AutoCylPressurePSI > RetainerPressureThresholdPSI)
            {
                epState = ValveState.Release;
                if (TripleValveState == ValveState.Lap)
                    TripleValveState = ValveState.Release;
            }
            base.Update(elapsedClockSeconds);
            if (AutoCylPressurePSI < RetainerPressureThresholdPSI)
            {
                epState = ValveState.Apply;
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (BrakeLine2PressurePSI - dp < AutoCylPressurePSI + dp)
                    dp = (BrakeLine2PressurePSI - AutoCylPressurePSI) * .5f;
                if (RetainerPressureThresholdPSI < AutoCylPressurePSI + dp)
                    dp = RetainerPressureThresholdPSI - AutoCylPressurePSI;
                BrakeLine2PressurePSI -= dp;
                AutoCylPressurePSI += dp;
            }
            if (epState != prevState)
            {
                switch (epState)
                {
                    case ValveState.Release: Car.SignalEvent(Event.TrainBrakePressureDecrease); break;
                    case ValveState.Apply: Car.SignalEvent(Event.TrainBrakePressureIncrease); break;
                }
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= MaxReleaseRatePSIpS * elapsedClockSeconds;
            }
            if (AutoCylPressurePSI < 0)
                AutoCylPressurePSI = 0;
            if (AutoCylPressurePSI < BrakeLine3PressurePSI)
                CylPressurePSI = BrakeLine3PressurePSI;
            else
                CylPressurePSI = AutoCylPressurePSI;
            float f = MaxBrakeForceN * Math.Min(CylPressurePSI / MaxCylPressurePSI, 1);
            if (f < MaxHandbrakeForceN * HandbrakePercent / 100)
                f = MaxHandbrakeForceN * HandbrakePercent / 100;
            Car.BrakeForceN = f;
            //Car.FrictionForceN += f;
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, PressureUnit unit)
        {
            string s = string.Format(" BC {0}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, true));
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus(PressureUnit unit)
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[9];
            rv[0] = "EP";
            rv[1] = string.Format("BC {0}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, false));
            rv[2] = string.Format("MRP {0}", FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, unit, false));
            rv[3] = string.Format("AR {0}", FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, unit, false));
            rv[4] = string.Format("ER {0}", FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, unit, false));
            rv[5] = string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, false));
            rv[6] = string.Format("State {0}", TripleValveState);
            rv[7] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[8] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
        }
        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            if (lead == null)
                SetUniformBrakePressures();
            else
                PropagateBrakeLinePressures(elapsedClockSeconds, lead, true);
        }
    }
}
