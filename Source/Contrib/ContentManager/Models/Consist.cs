// COPYRIGHT 2015 by the Open Rails project.
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
using System.Linq;
using System.Text;

namespace ORTS.ContentManager.Models
{
    public class Consist
    {
        public readonly string Name;
        public readonly string NumEngines; // using "+" between DPU sets
        public readonly string NumCars;
        public readonly float MaxSpeedMps;
        public readonly float LengthM = 0F;
        public readonly float WeightKG = 0F;
        public readonly float PowerW = 0F;
        public readonly float MaxTractiveForceN = 0F;
        public readonly float MaxBrakeForce = 0F;
        public readonly int NumOperativeBrakes = 0;

        public readonly IEnumerable<Car> Cars;

        public Consist(Content content)
        {
            Debug.Assert(content.Type == ContentType.Consist);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".con", StringComparison.OrdinalIgnoreCase))
            {
                var file = new ConsistFile(content.PathName);
                Name = file.Name;
                MaxSpeedMps = file.Train.TrainCfg.MaxVelocity.A;

                var EngCount = 0;
                var WagCount = 0;
                var Separator = ""; // when set, indicates that subsequent engines are in a separate block

                var CarList = new List<Car>();
                foreach (Wagon wag in file.Train.TrainCfg.WagonList)
                {
                    CarList.Add(new Car(wag));

                    if (wag.IsEngine)
                    {
                        EngCount++;
                    } else
                    {
                        if (EngCount > 0)
                        {
                            NumEngines = NumEngines + Separator + EngCount.ToString();
                            EngCount = 0; Separator = "+";
                        }
                        WagCount++;
                    }
                }
                if (EngCount > 0) { NumEngines = NumEngines + Separator + EngCount.ToString(); }
                if (NumEngines == null) { NumEngines = "0"; }
                NumCars = WagCount.ToString();
                Cars = CarList;
            }
        }

        public enum Direction{
            Forwards,
            Backwards,
        }

        public class Car
        {
            public readonly string ID;
            public readonly string Name;
            public readonly Direction Direction;

            internal Car(Wagon car)
            {
                ID = car.UiD.ToString();
                Name = car.Folder + "/" + car.Name;
                Direction = car.Flip ? Consist.Direction.Backwards : Consist.Direction.Forwards;
            }
        }
    }
}
