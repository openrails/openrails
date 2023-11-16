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

namespace Orts.Simulation.Signalling
{
    public class ObjectItemInfo
    {
        public enum ObjectItemType
        {
            Any,
            Signal,
            Speedlimit,
        }

        public enum ObjectItemFindState
        {
            None = 0,
            Object = 1,
            EndOfTrack = -1,
            PassedDanger = -2,
            PassedMaximumDistance = -3,
            TdbError = -4,
            EndOfAuthority = -5,
            EndOfPath = -6,
        }

        public ObjectItemType ObjectType;                     // type information
        public ObjectItemFindState ObjectState;               // state information

        public SignalObject ObjectDetails;                    // actual object 

        public float distance_found;
        public float distance_to_train;
        public float distance_to_object;

        public MstsSignalAspect signal_state;                   // UNKNOWN if type = speedlimit
        // set active by TRAIN
        public float speed_passenger;                // -1 if not set
        public float speed_freight;                  // -1 if not set
        public int speed_flag;
        public int speed_reset;
        // for signals: if = 1 no speed reduction; for speedposts: if = 0 standard; = 1 start of temp speedreduction post; = 2 end of temp speed reduction post
        public int speed_noSpeedReductionOrIsTempSpeedReduction;
        public int no_speedUpdate;
        public bool speed_isWarning;
        public float actual_speed;                   // set active by TRAIN

        public bool processed;                       // for AI trains, set active by TRAIN

        public ObjectItemInfo(SignalObject thisObject, float distance)
        {
            ObjectSpeedInfo speed_info;
            ObjectState = ObjectItemFindState.Object;

            distance_found = distance;

            ObjectDetails = thisObject;

            if (thisObject.Type == SignalObjectType.Signal)
            {
                ObjectType = ObjectItemType.Signal;
                signal_state = MstsSignalAspect.UNKNOWN;  // set active by TRAIN
                speed_passenger = -1;                      // set active by TRAIN
                speed_freight = -1;                      // set active by TRAIN
                speed_flag = 0;                       // set active by TRAIN
                no_speedUpdate = 0;                     // set active by TRAIN 
                speed_reset = 0;                      // set active by TRAIN
                speed_noSpeedReductionOrIsTempSpeedReduction = 0;
            }
            else
            {
                ObjectType = ObjectItemType.Speedlimit;
                signal_state = MstsSignalAspect.UNKNOWN;
                speed_info = thisObject.this_lim_speed(SignalFunction.SPEED);
                speed_passenger = speed_info.speed_pass;
                speed_freight = speed_info.speed_freight;
                speed_flag = speed_info.speed_flag;
                no_speedUpdate = speed_info.no_speedUpdate;
                speed_reset = speed_info.speed_reset;
                speed_noSpeedReductionOrIsTempSpeedReduction = speed_info.speed_noSpeedReductionOrIsTempSpeedReduction;
                speed_isWarning = speed_info.speed_isWarning;
            }
        }

        public ObjectItemInfo(ObjectItemFindState thisState)
        {
            ObjectState = thisState;
        }
    }
}
