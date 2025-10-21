// COPYRIGHT 2022 by the Open Rails project.
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
using Microsoft.Xna.Framework;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class InductionMotor : ElectricMotor
    {
        public float TargetForceN;
        public float EngineMaxSpeedMpS;
        public float OptimalAsyncSpeedRadpS = 1;
        public bool SlipControl;

        /// <summary>
        /// Motor drive frequency
        /// </summary>
        public float DriveSpeedRadpS;
        /// <summary>
        /// Maximum torque, as determined by throttle setting and force curves
        /// </summary>
        float requiredTorqueNm;

        public InductionMotor(Axle axle, MSTSLocomotive locomotive) : base(axle, locomotive)
        {
        }
        public override void Initialize()
        {
            base.Initialize();
        }
        public override double GetDevelopedTorqueNm(double motorSpeedRadpS)
        {
            return requiredTorqueNm * MathHelper.Clamp((float)(DriveSpeedRadpS - motorSpeedRadpS) / OptimalAsyncSpeedRadpS, -1, 1);
        }
        public override void Update(float timeSpan)
        {
            TargetForceN = Locomotive.TractiveForceN * AxleConnected.TractiveForceFraction;
            EngineMaxSpeedMpS = Locomotive.MaxSpeedMpS;
            SlipControl = Locomotive.SlipControlSystem == MSTSLocomotive.SlipControlType.Full;
            float linToAngFactor = AxleConnected.TransmissionRatio / AxleConnected.WheelRadiusM;
            if (SlipControl)
            {
                if (TargetForceN > 0) DriveSpeedRadpS = (AxleConnected.TrainSpeedMpS + AxleConnected.WheelSlipThresholdMpS * 0.95f) * linToAngFactor + OptimalAsyncSpeedRadpS;
                else if (TargetForceN < 0) DriveSpeedRadpS = (AxleConnected.TrainSpeedMpS - AxleConnected.WheelSlipThresholdMpS * 0.95f) * linToAngFactor - OptimalAsyncSpeedRadpS;
            }
            else
            {
                if (TargetForceN > 0) DriveSpeedRadpS = EngineMaxSpeedMpS * linToAngFactor + OptimalAsyncSpeedRadpS;
                else if (TargetForceN < 0) DriveSpeedRadpS = -EngineMaxSpeedMpS * linToAngFactor - OptimalAsyncSpeedRadpS;
            }
            requiredTorqueNm = Math.Abs(TargetForceN) * AxleConnected.WheelRadiusM / AxleConnected.TransmissionRatio;
            base.Update(timeSpan);
        }
    }
}
