// COPYRIGHT 2024 by the Open Rails project.
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
    public class PIDController
    {
        public float P { get; private set; } // Proportional component
        public float I { get; private set; } // Integral component
        public float D { get; private set; } // Derivative component
        public float MinValue=float.MinValue;
        public float MaxValue=float.MaxValue;
        public float MinIntegralValue = float.MinValue;
        public float MaxIntegralValue = float.MaxValue;
        public float DefaultValue;
        public bool Active;
        public float Value { get; private set; }
        public float LastError { get; private set; }
        public float TotalError { get; private set; }
        public PIDController(float p_coeff, float i_coeff, float d_coeff)
        {
            P = p_coeff;
            I = i_coeff;
            D = d_coeff;
        }
        public void Initialize()
        {
            Value = DefaultValue;
            LastError = 0;
            TotalError = 0;
        }
        public void SetCoefficients(float p, float i, float d)
        {
            P = p;
            I = i;
            D = d;
        }
        public void Update(float elapsedClockSeconds, float error)
        {
            if (!Active)
            {
                Active = true;
                Initialize();
            }

            float d_error = elapsedClockSeconds == 0 ? 0 : (error - LastError) / elapsedClockSeconds;
            if ((error > 0 && Value < MaxValue && Value < MaxIntegralValue) || (error < 0 && Value > MinValue && Value > MinIntegralValue))
            {
                TotalError += (error + LastError) * elapsedClockSeconds / 2;
            }

            float p_out = P * error;
            float i_out = I * TotalError;
            float d_out = D * d_error;

            Value = d_out + p_out + i_out;
            if (Value < MinValue) Value = MinValue;
            if (Value > MaxValue) Value = MaxValue;

            LastError = error;
        }
    }
}
