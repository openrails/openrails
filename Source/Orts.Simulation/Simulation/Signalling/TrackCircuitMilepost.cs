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
    public class TrackCircuitMilepost
    {
        public Milepost MilepostRef;                       // reference to milepost 
        public float[] MilepostLocation = new float[2];    // milepost location from both ends //
        public uint Idx;                                   // milepost Idx within TrItemTable // 

        public TrackCircuitMilepost(Milepost thisRef, float thisLocation0, float thisLocation1)
        {
            MilepostRef = thisRef;
            MilepostLocation[0] = thisLocation0;
            MilepostLocation[1] = thisLocation1;
        }
    }
}
