using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public enum IntegratorMethods
    {
        EulerBackward   = 0,
        EulerBackMod    = 1,
        EulerForward    = 2,
        RungeKutta2     = 3,
        RungeKutta4     = 4,
        NewtonRhapson   = 5,
        AdamsMoulton    = 6
    }
    /// <summary>
    /// Integrator class covers discrete integrator methods
    /// Some forward method needs to be implemented
    /// </summary>
    public class Integrator
    {
        float integralValue;
        float[] previousValues = new float[4];
        float[] previousStep = new float[4];
        float initialCondition;

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
        public int NumOfSubstepsPS { get { return numOfSubstepsPS; } }

        /// <summary>
        /// Max count of substeps when timespan dividing
        /// </summary>
        public int MaxSubsteps { set; get; }

        public Integrator()
        {
            Method = IntegratorMethods.EulerBackward;
            MinStep = 0.01f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            integralValue = 0.0f;
            initialCondition = 0.0f;
            MaxSubsteps = 10;
            for(int i = 0; i < 4; i++)
                previousValues[i] = 0.0f;
            oldTime = 0.0f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initCondition">Initial condition of integration</param>
        public Integrator(float initCondition)
        {
            Method = IntegratorMethods.EulerBackward;
            MinStep = 0.01f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            initialCondition = initCondition;
            integralValue = initialCondition;
            MaxSubsteps = 10;
            for (int i = 0; i < 4; i++)
                previousValues[i] = initCondition;
            oldTime = 0.0f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initCondition">Initial condition of integration</param>
        /// <param name="method">Method of integration</param>
        public Integrator(float initCondition, IntegratorMethods method)
        {
            Method = method;
            MinStep = 0.01f;
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            initialCondition = initCondition;
            integralValue = initialCondition;
            MaxSubsteps = 50;
            for (int i = 0; i < 4; i++)
                previousValues[i] = initCondition;
            oldTime = 0.0f;
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
        public float Integrate(float timeSpan, float value)
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

            if (timeSpan > MinStep)
            {
                count = Convert.ToInt32(Math.Round(timeSpan / MinStep, 0));
                if (count > MaxSubsteps)
                    count = MaxSubsteps;
                timeSpan = timeSpan / count;
                IsStepDividing = true;
                numOfSubstepsPS = count;

                if (numOfSubstepsPS > (MaxSubsteps / 2))
                    Method = IntegratorMethods.EulerBackMod;
                else
                    Method = IntegratorMethods.RungeKutta4;
            }
            else
            {
                IsStepDividing = false;
            }


            while ((step += timeSpan) <= end)
            {
                switch (Method)
                {
                    case IntegratorMethods.EulerBackward:
                        integralValue += timeSpan * value;
                        break;
                    case IntegratorMethods.EulerBackMod:
                        integralValue += timeSpan / 2.0f * (previousValues[0] + value);
                        previousValues[0] = value;
                        break;
                    case IntegratorMethods.EulerForward:
                        throw new NotImplementedException("Not implemented yet!");
                        break;
                    case IntegratorMethods.RungeKutta2:
                        //throw new NotImplementedException("Not implemented yet!");
                        k1 = integralValue + timeSpan / 2 * value;
                        k2 = 2 * (k1 - integralValue) / timeSpan;
                        integralValue += timeSpan * k2;
                        break;
                    case IntegratorMethods.RungeKutta4:
                        //throw new NotImplementedException("Not implemented yet!");
                        k1 = timeSpan * value;
                        k2 = k1 + timeSpan / 2.0f * value;
                        k3 = k1 + timeSpan / 2.0f * k2;
                        k4 = timeSpan * k3;
                        integralValue += (k1 + 2.0f * k2 + 2.0f * k3 + k4) / 6.0f;
                        break;
                    case IntegratorMethods.NewtonRhapson:
                        throw new NotImplementedException("Not implemented yet!");
                        break;
                    case IntegratorMethods.AdamsMoulton:
                        //prediction
                        float predicted = integralValue + timeSpan / 24.0f * (55.0f * previousValues[0] - 59.0f * previousValues[1] + 37.0f * previousValues[2] - 9.0f * previousValues[3]);
                        //correction
                        integralValue = integralValue + timeSpan / 24.0f * (9.0f * predicted + 19.0f * previousValues[0] - 5.0f * previousValues[1] + previousValues[2]);
                        for (int i = 3; i > 0; i--)
                        {
                            previousStep[i] = previousStep[i - 1];
                            previousValues[i] = previousValues[i - 1];
                        }
                        previousValues[0] = value;
                        previousStep[0] = timeSpan;
                        break;
                    default:
                        throw new NotImplementedException("Not implemented yet!");
                        break;
                }
                //To make sure the loop exits
                //if (count-- < 0)
                //    break;
            }
            //Limit if enabled
            if (isLimited)
            {
                return (integralValue <= min) ? (integralValue = min) : ((integralValue >= max) ? (integralValue = max) : integralValue);
            }
            else
                return integralValue;
        }
        /// <summary>
        /// Integrates given value in time. TimeSpan (integration step) is computed internally.
        /// </summary>
        /// <param name="elapsedClockSeconds">Time value in seconds</param>
        /// <param name="value">Value to integrate</param>
        /// <returns>Value of integration in elapsedClockSeconds time</returns>
        public float TimeIntegrate(float elapsedClockSeconds, float value)
        {
            float timeSpan = elapsedClockSeconds - oldTime;
            oldTime = elapsedClockSeconds;
            integralValue += timeSpan * value;
            if (isLimited)
            {
                return (integralValue <= min) ? min : ((integralValue >= max) ? max : integralValue);
            }
            else
                return integralValue;
        }
    }
}
