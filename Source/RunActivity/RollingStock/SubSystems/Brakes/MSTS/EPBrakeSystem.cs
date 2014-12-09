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

using System;
using ORTS.Common;

namespace ORTS
{

    public class EPBrakeSystem : AirTwinPipe
    {
        public EPBrakeSystem(TrainCar car)
            : base(car)
        {
            DebugType = "EP";
        }

        public override void UpdateTripleValveState(float controlPressurePSI)
        {
            base.UpdateTripleValveState(controlPressurePSI);
            if (Car.Train.BrakeLine4 >= 0 && TripleValveState == ValveState.Release)
                TripleValveState = ValveState.Lap;
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            var demandedAutoCylPressurePSI = Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * FullServPressurePSI;

            if (AutoCylPressurePSI < demandedAutoCylPressurePSI)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (BrakeLine2PressurePSI - AutoCylPressurePSI) / (1 + AuxBrakeLineVolumeRatio / AuxCylVolumeRatio);
                if (dp > demandedAutoCylPressurePSI - AutoCylPressurePSI)
                    dp = demandedAutoCylPressurePSI - AutoCylPressurePSI;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;

                Car.SignalEvent(Event.TrainBrakePressureIncrease);
            }
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, PressureUnit unit)
        {
            string s = string.Format(" BC {0}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, true));
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }
    }
}
