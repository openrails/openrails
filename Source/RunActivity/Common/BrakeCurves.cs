/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
///
/// Author: Charlie Salts (aka: CommanderMath)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Common
{
   /// <summary>
   /// Helper class for computing brake curves for use with train control systems. 
   /// </summary>
   class BrakeCurves
   {


      /// <summary>
      /// Given a train's current speed, the distance to a target, and the maximum allowed speed at that target, 
      /// as well as a desired deceleration value, return the speed the train *should* be travelling in order to
      /// "follow" the brake curve to the target.
      /// </summary>
      /// <param name="currentSpeed">Speed of the vehicle, in m/s.</param>
      /// <param name="targetDistance">Distance to the target, in metres.</param>
      /// <param name="targetSpeed">Maximum allowed speed at the target, in m/s.</param>
      /// <param name="nominalDeceleration">The desired/expected deceleration, in m/s². This must be larger than 0.</param>
      /// <returns></returns>
      public static float ComputeCurve(float currentSpeed, float targetDistance, float targetSpeed, float nominalDeceleration)
      {
         float returnValue = 0;

         if (nominalDeceleration <= 0)
         {
            throw new ArgumentException("nominalDeceleration must be larger than 0");
         }

         // this part covers the case where the target speed is *not* zero
         float computedDistance = (targetDistance + (targetSpeed * targetSpeed) / (2 * nominalDeceleration));

         // solve for desired speed
         returnValue = (float) Math.Sqrt(2 * nominalDeceleration * computedDistance);
         
         return returnValue;
      }
   }
}
