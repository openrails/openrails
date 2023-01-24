// COPYRIGHT 2021 by the Open Rails project.
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

using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Simulation.Signalling
{
    public class SignalWorldObject
    {
        public string SFileName;
        public Dictionary<uint, uint> HeadReference;     // key=TDBIndex, value=headindex
        public bool[] HeadsSet;                          // Flags heads which are set
        public bool[] FlagsSet;                          // Flags signal-flags which are set
        public bool[] FlagsSetBackfacing;                // Flags signal-flags which are set
        //    for backfacing signal
        public List<int> Backfacing = new List<int>();   // Flags heads which are backfacing

        public SignalWorldObject(SignalObj SignalWorldItem, SignalConfigurationFile sigcfg)
        {
            SignalShape thisCFGShape;

            HeadReference = new Dictionary<uint, uint>();

            // set flags with length to number of possible SubObjects type

            FlagsSet = new bool[SignalShape.SignalSubObj.SignalSubTypes.Count];
            FlagsSetBackfacing = new bool[SignalShape.SignalSubObj.SignalSubTypes.Count];
            for (uint iFlag = 0; iFlag < FlagsSet.Length; iFlag++)
            {
                FlagsSet[iFlag] = false;
                FlagsSetBackfacing[iFlag] = false;
            }

            // get filename in Uppercase

            SFileName = Path.GetFileName(SignalWorldItem.FileName).ToUpperInvariant();

            // search defined shapes in SIGCFG to find signal definition

            if (sigcfg.SignalShapes.TryGetValue(SFileName, out thisCFGShape))
            {

                HeadsSet = new bool[thisCFGShape.SignalSubObjs.Count];

                // loop through all heads and check SubObj flag per bit to check if head is set

                uint iMask = 1;

                for (int iHead = 0; iHead < thisCFGShape.SignalSubObjs.Count; iHead++)
                {
                    HeadsSet[iHead] = false;
                    uint headSet = SignalWorldItem.SignalSubObj & iMask;
                    SignalShape.SignalSubObj thisSubObjs = thisCFGShape.SignalSubObjs[iHead];
                    if (headSet != 0)
                    {

                        // set head, and if head is flag, also set flag

                        HeadsSet[iHead] = true;

                        if (thisSubObjs.BackFacing)
                        {
                            Backfacing.Add(iHead);
                            if (thisSubObjs.SignalSubType >= 1)
                            {
                                FlagsSetBackfacing[thisSubObjs.SignalSubType] = true;
                            }
                        }
                        else if (thisSubObjs.SignalSubType >= 1)
                        {
                            FlagsSet[thisSubObjs.SignalSubType] = true;
                        }
                    }
                    iMask = iMask << 1;
                }

                // get TDB and head reference from World file

                foreach (SignalUnit signalUnitInfo in SignalWorldItem.SignalUnits.Units)
                {
                    uint TrItemRef = signalUnitInfo.TrItem;
                    uint HeadRef = Convert.ToUInt32(signalUnitInfo.SubObj);
                    HeadReference.Add(TrItemRef, HeadRef);
                }
            }
            else
            {
                Trace.TraceWarning("Signal not found : {0} n", SFileName);
            }
        }

        /// <summary>
        /// Constructor for copy
        /// </summary>
        public SignalWorldObject(SignalWorldObject copy)
        {
            SFileName = String.Copy(copy.SFileName);
            Backfacing = copy.Backfacing;

            HeadsSet = new bool[copy.HeadsSet.Length];
            FlagsSet = new bool[copy.FlagsSet.Length];
            FlagsSetBackfacing = new bool[copy.FlagsSet.Length];
            copy.HeadsSet.CopyTo(HeadsSet, 0);
            copy.FlagsSet.CopyTo(FlagsSet, 0);
            copy.FlagsSetBackfacing.CopyTo(FlagsSet, 0);

            HeadReference = new Dictionary<uint, uint>();
            foreach (KeyValuePair<uint, uint> thisRef in copy.HeadReference)
            {
                HeadReference.Add(thisRef.Key, thisRef.Value);
            }
        }
    }
}
