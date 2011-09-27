/// COPYRIGHT 2011 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ORTS
{
    /// <summary>
    /// Track occupancy counter.
    /// </summary>
    public class TrackCircuit
    {
        int NumTrains = 0;
        public bool IsOccupied()
        {
            return NumTrains > 0;
        }
        public void IncTrains()
        {
            NumTrains++;
            //Trace.WriteLine(string.Format("inctrains {0}", NumTrains));
        }
        public void DecTrains()
        {
            NumTrains--;
            if (NumTrains < 0)
                NumTrains = 0;
            //Trace.WriteLine(string.Format("dectrains {0}", NumTrains));
        }
    }
}
