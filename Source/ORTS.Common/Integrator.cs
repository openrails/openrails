// COPYRIGHT 2011 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS.Common
{
    public enum IntegratorMethods
    {
        EulerBackward = 0,
        EulerBackMod = 1,
        EulerForward = 2,
        RungeKutta2 = 3,
        RungeKutta4 = 4,
        NewtonRhapson = 5,
        AdamsMoulton = 6
    }
    /// <summary>
    /// Integrator class covers discrete integrator methods
    /// Some forward method needs to be implemented
    /// </summary>
    public class Integrator
    {
        float integralValue;
        float[] previousValues = new float[100];
        float[] previousStep = new float[100];
        float initialCondition;

        float derivation;

        float prevDerivation;

        public IntegratorMethods Method;

        float max;
        float min;
        bool isLimited;
        float oldTime;

        /// <summary>
        /// Initial condition acts as a Value at the beginning of the integration
        /// </summary>
        public float InitialCondition { set { initialCondition = value; } get { return initialCondition; } }
        /// <summary>
        /// Integrated value
        /// </summary>
        public float Value { get { return integralValue; } }
        /// <summary>
        /// Upper limit of the Value. Cannot be smaller than Min. Max is considered only if IsLimited is true
        /// </summary>
        public float Max
        {
            set
            {
                if (max <= min)
                    throw new NotSupportedException("Maximum must be greater than minimum");
                max = value;

            }
            get { return max; }
        }
        /// <summary>
        /// Lower limit of the Value. Cannot be greater than Max. Min is considered only if IsLimited is true
        /// </summary>
        public float Min
        {
            set
            {
                if (max <= min)
                    throw new NotSupportedException("Minimum must be smaller than maximum");
                min = value;
            }
            get { return min; }
        }
        /// <summary>
        /// Determines limitting according to Max and Min values
        /// </summary>
        public bool IsLimited { set { isLimited = value; } get { return isLimited; } }

        /// <summary>
        /// Minimal step of integration
        /// </summary>
        public float MinStep { set; get; }
        public bool IsStepDividing { set; get; }
        int numOfSubstepsPS = 1;
        int waitBeforeSpeedingUp;
        public int NumOfSubstepsPS { get { return numOfSubstepsPS; } }

        /// <summary>
        /// Max count of substeps when timespan dividing
        /// </summary>
        public int MaxSubsteps { set; get; }

        public float Error { set; get; }

        public Integrator()
        {
            Method = IntegratorMethods.EulerBackward;
            MinStep = 0.001f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            integralValue = 0.0f;
            initialCondition = 0.0f;
            MaxSubsteps = 300;
            for (int i = 0; i < previousValues.Length; i++)
                previousValues[i] = 0.0f;
            oldTime = 0.0f;
            Error = 0.001f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initCondition">Initial condition of integration</param>
        public Integrator(float initCondition)
        {
            Method = IntegratorMethods.EulerBackward;
            MinStep = 0.001f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            initialCondition = initCondition;
            integralValue = initialCondition;
            MaxSubsteps = 300;
            for (int i = 0; i < previousValues.Length; i++)
                previousValues[i] = initCondition;
            oldTime = 0.0f;
            Error = 0.001f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initCondition">Initial condition of integration</param>
        /// <param name="method">Method of integration</param>
        public Integrator(float initCondition, IntegratorMethods method)
        {
            Method = method;
            MinStep = 0.001f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            initialCondition = initCondition;
            integralValue = initialCondition;
            MaxSubsteps = 300;
            for (int i = 0; i < previousValues.Length; i++)
                previousValues[i] = initCondition;
            oldTime = 0.0f;
            Error = 0.001f;
        }

        public Integrator(Integrator copy)
        {
            Method = copy.Method;
            MinStep = copy.MinStep;
            max = copy.max;
            min = copy.min;
            isLimited = copy.isLimited;
            integralValue = copy.integralValue;
            initialCondition = copy.initialCondition;
            MaxSubsteps = copy.MaxSubsteps;
            for (int i = 0; i < previousValues.Length; i++)
                previousValues[i] = initialCondition;
            oldTime = 0.0f;
            Error = copy.Error;
        }
        /// <summary>
        /// Resets the Value to its InitialCondition
        /// </summary>
        public void Reset()
        {
            integralValue = initialCondition;
        }
        /// <summary>
        /// Integrates given value with given time span
        /// </summary>
        /// <param name="timeSpan">Integration step or timespan in seconds</param>
        /// <param name="value">Value to integrate</param>
        /// <returns>Value of integration in the next step (t + timeSpan)</returns>
        public float Integrate(float timeSpan, Func<float, float> value)
        {
            float step = 0.0f;
            float end = timeSpan;
            int count = 0;

            float k1, k2, k3, k4 = 0;

            //Skip when timeSpan is less then zero
            if (timeSpan <= 0.0f)
            {
                return integralValue;
            }
            if (Math.Abs(prevDerivation) > Error)
            {
                count = ++numOfSubstepsPS;
                if (count > MaxSubsteps)
                    count = MaxSubsteps;
                waitBeforeSpeedingUp = 100;
            }
            else
            {
                if (--waitBeforeSpeedingUp <= 0)    //wait for a while before speeding up the integration
                {
                    count = Math.Max(--numOfSubstepsPS, 1);

                    waitBeforeSpeedingUp = 10;      //not so fast ;)
                }
                else
                    count = numOfSubstepsPS;
            }

            timeSpan = timeSpan / (float)count;
            numOfSubstepsPS = count;
            IsStepDividing = count > 1;


            #region SOLVERS
            //while ((step += timeSpan) <= end)
            for (step = timeSpan; step <= end; step += timeSpan)
            {
                switch (Method)
                {
                    case IntegratorMethods.EulerBackward:
                        integralValue += (derivation = timeSpan * value(integralValue));
                        break;
                    case IntegratorMethods.EulerBackMod:
                        integralValue += (derivation = timeSpan / 2.0f * (previousValues[0] + value(integralValue)));
                        previousValues[0] = value(integralValue);
                        break;
                    case IntegratorMethods.EulerForward:
                        throw new NotImplementedException("Not implemented yet!");

                    case IntegratorMethods.RungeKutta2:
                        k1 = integralValue + timeSpan / 2 * value(integralValue);
                        k2 = 2 * (k1 - integralValue) / timeSpan;
                        integralValue += (derivation = timeSpan * k2);
                        break;
                    case IntegratorMethods.RungeKutta4:
                        k1 = value(integralValue);
                        k2 = value(integralValue + k1 * timeSpan / 2.0f);
                        k3 = value(integralValue + k2 * timeSpan / 2.0f);
                        k4 = value(integralValue + k3 * timeSpan);
                        integralValue += (derivation = (k1 + 2.0f * k2 + 2.0f * k3 + k4) * timeSpan / 6.0f);
                        break;
                    case IntegratorMethods.NewtonRhapson:
                        throw new NotImplementedException("Not implemented yet!");

                    case IntegratorMethods.AdamsMoulton:
                        //prediction
                        float predicted = integralValue + timeSpan / 24.0f * (55.0f * previousValues[0] - 59.0f * previousValues[1] + 37.0f * previousValues[2] - 9.0f * previousValues[3]);
                        //correction
                        integralValue = integralValue + timeSpan / 24.0f * (9.0f * predicted + 19.0f * previousValues[0] - 5.0f * previousValues[1] + previousValues[2]);
                        for (int i = previousStep.Length - 1; i > 0; i--)
                        {
                            previousStep[i] = previousStep[i - 1];
                            previousValues[i] = previousValues[i - 1];
                        }
                        previousValues[0] = value(integralValue);
                        previousStep[0] = timeSpan;
                        break;
                    default:
                        throw new NotImplementedException("Not implemented yet!");

                }
                //To make sure the loop exits
                //if (count-- < 0)
                //    break;
            }

            #endregion

            prevDerivation = derivation;

            //Limit if enabled
            if (isLimited)
            {
                return (integralValue <= min) ? (integralValue = min) : ((integralValue >= max) ? (integralValue = max) : integralValue);
            }
            else
                return integralValue;
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(integralValue);
        }

        public void Restore(BinaryReader inf)
        {
            integralValue = inf.ReadSingle();

            for (int i = 0; i < 4; i++)
                previousValues[i] = integralValue;
        }

    }
}
