// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public abstract class MSTSBrakeSystem : BrakeSystem
    {
        public static BrakeSystem Create(string type, TrainCar car)
        {
            switch (type)
            {
                case "manual_braking": return new ManualBraking(car);
                case "straight_vacuum_single_pipe": return new StraightVacuumSinglePipe(car);
                case "vacuum_twin_pipe":
                case "vacuum_single_pipe": return new VacuumSinglePipe(car);
                case "air_twin_pipe": return new AirTwinPipe(car);
                case "air_single_pipe": return new AirSinglePipe(car);
                case "ecp":
                case "ep": return new EPBrakeSystem(car);
                case "air_piped":
                case "vacuum_piped": return new SingleTransferPipe(car);
                default: return new SingleTransferPipe(car);
            }
        }

        public abstract void Parse(string lowercasetoken, STFReader stf);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void InitializeFromCopy(BrakeSystem copy);
    }
}
