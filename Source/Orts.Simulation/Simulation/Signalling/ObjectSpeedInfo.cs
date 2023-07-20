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
    public class ObjectSpeedInfo
    {
        public float speed_pass;
        public float speed_freight;
        public int speed_flag;
        public int no_speedUpdate;
        public int speed_reset;
        public int speed_noSpeedReductionOrIsTempSpeedReduction;
        public bool speed_isWarning;

        public ObjectSpeedInfo(float pass, float freight, bool asap, bool reset, int nospeedreductionOristempspeedreduction, bool isWarning, bool nospeedupdate = false)
        {
            speed_pass = pass;
            speed_freight = freight;
            speed_flag = asap ? 1 : 0;
            speed_reset = reset ? 1 : 0;
            no_speedUpdate = nospeedupdate ? 1 : 0;
            speed_noSpeedReductionOrIsTempSpeedReduction = nospeedreductionOristempspeedreduction;
            speed_isWarning = isWarning;
        }
    }
}
