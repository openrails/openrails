// COPYRIGHT 2025 by the Open Rails project.
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

namespace ORTS.Common
{
    /// <summary>
    /// Modified PID controller that converts requested force/acceleration to throttle or brake percent
    /// </summary>
    public class AccelerationController
    {
        public float Percent { get; private set; }
        public float PPercent { get; private set; }
        public float IPercent { get; private set; }
        public float DPercent { get; private set; }
        private float TotalError;
        private float LastTarget;
        private float LastError;
        private bool active;
        private readonly float[] Coefficients;
        private float ProportionalFactor;
        private float IntegralFactor;
        private float DerivativeFactor;
        public float Tolerance;
        public bool Active
        {
            set
            {
                if (active != value)
                {
                    LastTarget = 0;
                    LastError = 0;
                    TotalError = 0;
                    Percent = 0;
                    active = value;
                }
            }
            get
            {
                return active;
            }
        }
        public AccelerationController(float p, float i, float d = 0)
        {
            Coefficients = new float[] { 100 * p, 100 * i, 100 * d };
        }
        protected AccelerationController(AccelerationController o)
        {
            Coefficients = o.Coefficients;
        }
        public AccelerationController Clone()
        {
            return new AccelerationController(this);
        }
        /// <summary>
        /// Adjust PID coefficients according to the maximum force/acceleration
        /// </summary>
        /// <param name="maxAccelerationMpSS">Maximum force or acceleration developed with controller at 100%</param>
        public void Adjust(float maxAccelerationMpSS)
        {
            ProportionalFactor = Coefficients[0] / maxAccelerationMpSS;
            IntegralFactor = Coefficients[1] / maxAccelerationMpSS;
            DerivativeFactor = Coefficients[2] / maxAccelerationMpSS;
        }
        public void Update(float elapsedClockSeconds, float targetAccelerationMpSS, float currentAccelerationMpSS, float minPercent = 0, float maxPercent = 100)
        {
            if (!Active) Active = true;
            float error = targetAccelerationMpSS - currentAccelerationMpSS;
            TotalError += (error + LastError) * elapsedClockSeconds / 2;
            PPercent = ProportionalFactor * targetAccelerationMpSS;
            IPercent = IntegralFactor * TotalError;
            DPercent = elapsedClockSeconds > 0 && DerivativeFactor > 0 ? DerivativeFactor * (error - LastError) / elapsedClockSeconds : 0;
            Percent = PPercent + IPercent + DPercent;
            if (Percent <= minPercent)
            {
                if (PPercent > minPercent && IntegralFactor > 0) TotalError = (minPercent - PPercent) / IntegralFactor;
                else if (TotalError < 0) TotalError = 0;
                Percent = minPercent;
            }
            if (Percent >= maxPercent)
            {
                if (PPercent < maxPercent && IntegralFactor > 0) TotalError = (maxPercent - PPercent) / IntegralFactor;
                else if (TotalError > 0) TotalError = 0;
                Percent = maxPercent;
            }
            LastTarget = targetAccelerationMpSS;
            LastError = error;
        }
    }
}
