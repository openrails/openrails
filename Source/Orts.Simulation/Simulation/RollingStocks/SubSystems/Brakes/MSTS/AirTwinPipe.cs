﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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


namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class AirTwinPipe : AirSinglePipe
    {
        public AirTwinPipe(TrainCar car)
            : base(car)
        {
            TwoPipes = true;
            MRPAuxResCharging = true;
            DebugType = "2P";
        }
    }
}
