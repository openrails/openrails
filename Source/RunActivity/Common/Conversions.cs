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
        public static float FromMpS(float MpS)
        {
            return MpS * (0.000621371192f /* mile/M */ * 3600f /* sec/hr */ );
        }
    }

    public class MpS
    {
        public static float FromMpH(float MpH)
        {
            return MpH / (0.000621371192f /* mile/M */ * 3600f /* sec/hr */ );
        }
    }
}
