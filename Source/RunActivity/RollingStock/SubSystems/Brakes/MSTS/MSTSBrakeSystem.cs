// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using MSTS.Parsers;

namespace ORTS
{
    public abstract class MSTSBrakeSystem : BrakeSystem
    {
        public static BrakeSystem Create(string type, TrainCar car)
        {
            if (type != null && type.StartsWith("vacuum"))
                return new VacuumSinglePipe(car);
            else if (type != null && type == "ep")
                return new EPBrakeSystem(car);
            else if (type != null && type == "air_twin_pipe")
                return new AirTwinPipe(car);
            else
                return new AirSinglePipe(car);
        }

        public abstract void Parse(string lowercasetoken, STFReader stf);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void InitializeFromCopy(BrakeSystem copy);
    }
}
