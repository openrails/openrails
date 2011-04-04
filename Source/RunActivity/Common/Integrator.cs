using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    /// <summary>
    /// 
    /// Integrator class is basic square-rule integrator (1st order basic euler integration)
    /// Some forward method needs to be implemented
    /// </summary>
    public class Integrator
    {
        float integralValue;
        float previousValue;
        float initialCondition;
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

        public Integrator()
        {
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            integralValue = 0.0f;
            initialCondition = 0.0f;
            previousValue = 0.0f;
            oldTime = 0.0f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initCondition">Initial condition of integration</param>
        public Integrator(float initCondition)
        {
            max = 1000.0f;
            min = -1000.0f;
            isLimited = false;
            initialCondition = initCondition;
            integralValue = initialCondition;
            previousValue = initialCondition;
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
            //Forward Euler formula
            integralValue += timeSpan * value;

            //Another backward Euler
            //integralValue = integralValue * (1.0f + (float)Math.Sqrt(1.0f - 2.0f * timeSpan / (integralValue * integralValue))) / 2.0f;
            
            //Backward Euler formula using prediction-correction
            //float prediction = 0.0f;
            //prediction = integralValue + timeSpan * value;
            //integralValue += timeSpan * prediction;

            //Trapezoid ODE method
            //integralValue *= timeSpan / 2.0f * (value + previousValue);
            
            //integralValue = previous + timeSpan * value;
            //integralValue = previous + timeSpan * (previous + integralValue) / 2.0f;
            //integralValue = previous + timeSpan * (previous + integralValue) / 2.0f;

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
