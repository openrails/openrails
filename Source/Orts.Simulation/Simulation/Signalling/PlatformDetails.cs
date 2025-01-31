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

using System;
using System.Collections.Generic;

namespace Orts.Simulation.Signalling
{
    public class PlatformDetails
    {
        public List<int> TCSectionIndex = new List<int>();
        public int[] PlatformReference = new int[2];
        public float[,] TCOffset = new float[2, 2];
        public float[] nodeOffset = new float[2];
        public float Length;
        public int[] EndSignals = new int[2] { -1, -1 };
        public float[] DistanceToSignals = new float[2];
        public string Name;
        public uint MinWaitingTime;
        public int NumPassengersWaiting;
        public bool[] PlatformSide = new bool[2] { false, false };
        public int PlatformFrontUiD = -1;

        public PlatformDetails(int platformReference)
        {
            PlatformReference[0] = platformReference;
        }

        /// <summary>
        /// Constructor for copy
        /// </summary>
        public PlatformDetails(PlatformDetails orgDetails)
        {
            foreach (int sectionIndex in orgDetails.TCSectionIndex)
            {
                TCSectionIndex.Add(sectionIndex);
            }

            orgDetails.PlatformReference.CopyTo(PlatformReference, 0);
            TCOffset[0, 0] = orgDetails.TCOffset[0, 0];
            TCOffset[0, 1] = orgDetails.TCOffset[0, 1];
            TCOffset[1, 0] = orgDetails.TCOffset[1, 0];
            TCOffset[1, 1] = orgDetails.TCOffset[1, 1];
            orgDetails.nodeOffset.CopyTo(nodeOffset, 0);
            Length = orgDetails.Length;
            orgDetails.EndSignals.CopyTo(EndSignals, 0);
            orgDetails.DistanceToSignals.CopyTo(DistanceToSignals, 0);
            Name = orgDetails.Name;
            MinWaitingTime = orgDetails.MinWaitingTime;
            NumPassengersWaiting = orgDetails.NumPassengersWaiting;
            PlatformSide[0] = orgDetails.PlatformSide[0];
            PlatformSide[1] = orgDetails.PlatformSide[1];
        }
    }
}
