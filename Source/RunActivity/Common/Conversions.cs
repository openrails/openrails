/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
	public class MpH
    {
        public static float FromKpH(float KpH) {
            return KpH * (0.621371192f /* mile/M */ );
        }

        public static float FromMpS(float MpS)
		{
			return MpS * (0.000621371192f /* mile/M */ * 3600f /* sec/hr */ );
		}

		public static float ToMpS(float MpH)
		{
			return MpH / (0.000621371192f /* mile/M */ * 3600f /* sec/hr */ );
		}
	}

	public class KpH
	{
		public static float FromMpS(float MpS)
		{
			return MpS * (0.001f /* kilometer/M */ * 3600f /* sec/hr */ );
		}

		public static float ToMpS(float MpH)
		{
			return MpH / (0.001f /* kilometer/M */ * 3600f /* sec/hr */ );
		}
	}

	public class MpS
    {
		public static float FromMpS(float speed, bool metric)
		{
			return metric ? KpH.FromMpS(speed) : MpH.FromMpS(speed);
		}

		public static float ToMpS(float speed, bool metric)
		{
			return metric ? KpH.ToMpS(speed) : MpH.ToMpS(speed);
		}
	}

#if NEW_SIGNALLING
	public class Miles
	{
		public static float FromM(float distance, bool metric)
		{
			return metric ? distance : (0.000621371192f * distance);
		}
		public static float toM(float distance, bool metric)
		{
			return metric ? distance : (distance / 0.000621371192f);
		}
	}


	public class FormatStrings
	{
        public static string FormatSpeed(float speed, bool metric)
        {
            return String.Format(metric ? "{0:F1}kph" : "{0:F1}mph", MpS.FromMpS(speed, metric));
        }

        public static string FormatDistance(float distance, bool metric)
        {
            if (metric)
            {
                // <0.1 kilometers, show meters.
                if (Math.Abs(distance) < 100)
                    return String.Format("{0:N0}m", distance);
                return String.Format("{0:F1}km", distance / 1000.000);
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < 160.9344)
                return String.Format("{0:N0}yd", distance * 1.093613298337708);
            return String.Format("{0:F1}mi", distance / 1609.344);
        }
	}
#endif		
}
