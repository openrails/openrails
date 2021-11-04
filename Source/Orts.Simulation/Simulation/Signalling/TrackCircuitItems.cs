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

using Orts.Formats.OR;
using System.Collections.Generic;

namespace Orts.Simulation.Signalling
{
    public class TrackCircuitItems
    {
        public List<TrackCircuitSignalList>[] TrackCircuitSignals = new List<TrackCircuitSignalList>[2];   // List of signals (per direction and per type) //
        public TrackCircuitSignalList[] TrackCircuitSpeedPosts = new TrackCircuitSignalList[2];            // List of speedposts (per direction) //
        public List<TrackCircuitMilepost> TrackCircuitMileposts = new List<TrackCircuitMilepost>();        // List of mileposts //

#if ACTIVITY_EDITOR
        // List of all Element coming from OR configuration in a generic form.
        public List<TrackCircuitElement> TrackCircuitElements = new List<TrackCircuitElement>();
#endif

        public TrackCircuitItems(int ORTSSignalTypes)
        {
            TrackCircuitSignalList thisList;

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                List<TrackCircuitSignalList> TrackSignalLists = new List<TrackCircuitSignalList>();
                for (int fntype = 0; fntype < ORTSSignalTypes; fntype++)
                {
                    thisList = new TrackCircuitSignalList();
                    TrackSignalLists.Add(thisList);
                }
                TrackCircuitSignals[iDirection] = TrackSignalLists;

                thisList = new TrackCircuitSignalList();
                TrackCircuitSpeedPosts[iDirection] = thisList;
            }
        }
    }
}
