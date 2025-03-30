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

// MSTS Wheels and ORTS Axles
// * ORTS uses unpowered and drive axles. Eg. 2 and 4 for an ES44C4
// * MSTS uses a mix of wheels and axle. The Wagon section has the total,
//   the Engine section the driven only.
//   Values greater than 6 are wheels, less than 6 are axles. Some are
//   intentionally wrong to work around MSTS issues.

using Orts.Formats.Msts;
using System;
using System.Diagnostics;

namespace ORTS.ContentManager.Models
{
    public enum CarType
    {
        Engine,
        Wagon,
    }

    public class Car
    {
        public readonly CarType Type;
        public readonly string SubType;
        public readonly string Name;
        public readonly string Description;
        public readonly float MassKG;
        public readonly float LengthM;
        public readonly int NumDriveAxles;
        public readonly int NumAllAxles;
        public readonly float MaxBrakeForceN;
        public readonly float MaxPowerW;
        public readonly float MaxForceN;
        public readonly float MaxSpeedMps;
        public readonly float MinCouplerStrengthN;
        public readonly float MinDerailForceN;

        public Car(Content content)
        {
            Debug.Assert(content.Type == ContentType.Car);

            const float GravitationalAccelerationMpS2 = 9.80665f;

            int ortsEngAxles = -1;  // not present

            // .eng files also have a wagon block
            var wagFile = new WagonFile(content.PathName);
            Type = CarType.Wagon;
            SubType = wagFile.WagonType;
            Name = wagFile.Name;
            MassKG = wagFile.MassKG;
            LengthM = wagFile.WagonSize.LengthM;
            MaxBrakeForceN = wagFile.MaxBrakeForceN;
            MinCouplerStrengthN = wagFile.MinCouplerStrengthN;

            if (System.IO.Path.GetExtension(content.PathName).Equals(".eng", StringComparison.OrdinalIgnoreCase))
            {
                var engFile = new EngineFile(content.PathName);
                Type = CarType.Engine;
                SubType = engFile.EngineType;
                Name = engFile.Name;
                MaxPowerW = engFile.MaxPowerW;
                MaxForceN = engFile.MaxForceN;
                MaxSpeedMps = engFile.MaxSpeedMps;
                Description = engFile.Description;

                // see MSTSLocomotive.Initialize()
                ortsEngAxles = engFile.NumDriveAxles;
                if (ortsEngAxles >= 0) { NumDriveAxles = ortsEngAxles; }
                else if (engFile.NumEngWheels >= 7f) { NumDriveAxles = (int)(engFile.NumEngWheels / 2f); }
                else if (engFile.NumEngWheels > 0f) { NumDriveAxles = (int)engFile.NumEngWheels; }
                else { NumDriveAxles = 4; }
            }

            // see MSTSWagon.LoadFromWagFile()
            if (ortsEngAxles >= 0 && wagFile.NumWagAxles >= 0) { NumAllAxles = ortsEngAxles + wagFile.NumWagAxles; }
            else if (wagFile.NumWagAxles >= 0) { NumAllAxles = wagFile.NumWagAxles; }
            else if (wagFile.NumWagWheels >= 7f) { NumAllAxles = (int)(wagFile.NumWagWheels / 2f); }
            else if (wagFile.NumWagWheels > 0f) { NumAllAxles = (int)wagFile.NumWagWheels; }
            else { NumAllAxles = 4; }
            if (NumDriveAxles > NumAllAxles) { NumAllAxles = NumDriveAxles; }

            // see TrainCar.UpdateTrainDerailmentRisk()
            var numWheels = NumAllAxles * 2;
            if (numWheels > 0) { MinDerailForceN = MassKG / numWheels * GravitationalAccelerationMpS2; }
        }
    }
}
