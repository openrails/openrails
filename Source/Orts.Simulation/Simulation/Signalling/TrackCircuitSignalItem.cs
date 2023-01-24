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

namespace Orts.Simulation.Signalling
{
    public class TrackCircuitSignalItem
    {
        public ObjectItemInfo.ObjectItemFindState SignalState;  // returned state // 
        public SignalObject SignalRef;            // related SignalObject     //
        public float SignalLocation;              // relative signal position //

        /// <summary>
        /// Constructor setting object
        /// </summary>
        public TrackCircuitSignalItem(SignalObject thisRef, float thisLocation)
        {
            SignalState = ObjectItemInfo.ObjectItemFindState.Object;
            SignalRef = thisRef;
            SignalLocation = thisLocation;
        }

        /// <summary>
        /// Constructor setting state
        /// </summary>
        public TrackCircuitSignalItem(ObjectItemInfo.ObjectItemFindState thisState)
        {
            SignalState = thisState;
        }
    }
}
