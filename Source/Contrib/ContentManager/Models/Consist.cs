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
using System.IO;

namespace ORTS.ContentManager.Models
{
    public class Consist
    {
        public readonly string Name;
        public readonly string NumEngines; // using "+" between DPU sets
        public readonly string NumCars;
        public readonly float MaxSpeedMps;
        public readonly float LengthM = 0F;
        public readonly int NumAxles = 0;
        public readonly float MassKG = 0F;
        public readonly float MaxPowerW = 0F;
        public readonly float MaxTractiveForceN = 0F;
        public readonly float MaxBrakeForce = 0F;
        public readonly int NumOperativeBrakes = 0;
        public readonly float MinCouplerStrengthN = 9.999e8f;  // impossible high force
        public readonly float MinDerailForceN = 9.999e8f;  // impossible high force

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

                var basePath = System.IO.Path.Combine(System.IO.Path.Combine(content.Parent.PathName, "Trains"), "Trainset");

                var CarList = new List<Car>();
                foreach (Wagon wag in file.Train.TrainCfg.WagonList)
                {
                    float wagonMassKG = 0; int ortsEngAxles = -1; int numDriveAxles = 0; int numAllAxles = 0;
                    try
                    {
                        var fileType = wag.IsEngine ? ".eng" : ".wag";
                        var filePath = System.IO.Path.Combine(System.IO.Path.Combine(basePath, wag.Folder), wag.Name + fileType);
                        var wagonFile = new WagonFile(filePath);
                        var engFile = wag.IsEngine ? new EngineFile(filePath) : null;

                        LengthM += wagonFile.WagonSize.LengthM;
                        MassKG += wagonFile.MassKG;
                        wagonMassKG = wagonFile.MassKG;
                        MaxBrakeForce += wagonFile.MaxBrakeForceN;
                        MinCouplerStrengthN = Math.Min(MinCouplerStrengthN, wagonFile.MinCouplerStrengthN);
                        if (wagonFile.MaxBrakeForceN > 0) { NumOperativeBrakes++; }

                        if (wag.IsEngine)
                        {
                            // see MSTSLocomotive.Initialize()
                            ortsEngAxles = engFile.NumDriveAxles;
                            if (ortsEngAxles >= 0) { numDriveAxles = ortsEngAxles; }
                            else if (engFile.NumEngWheels > 7f) { numDriveAxles = (int)(engFile.NumEngWheels / 2f); }
                            else if (engFile.NumEngWheels > 0) { numDriveAxles = (int)engFile.NumEngWheels; }
                            else { numDriveAxles = 4; }

                            if (engFile.MaxForceN > 25000)  // exclude legacy driving trailers / cab-cars
                            {
                                EngCount++;
                                MaxPowerW += engFile.MaxPowerW;
                                MaxTractiveForceN += engFile.MaxForceN;
                            }
                            else { WagCount++; }
                        }
                        else if (!wag.IsEOT && wagonFile.WagonSize.LengthM > 1.1) // exclude legacy EOT
                        {
                            WagCount++;
                        }

                        // see MSTSWagon.LoadFromWagFile()
                        if (ortsEngAxles >= 0 && wagonFile.NumWagAxles >= 0) { numAllAxles = ortsEngAxles + wagonFile.NumWagAxles; }
                        else if (wagonFile.NumWagAxles >= 0) { numAllAxles =  wagonFile.NumWagAxles; }
                        else if (wagonFile.NumWagWheels >= 7f) { numAllAxles = (int)(wagonFile.NumWagWheels / 2f); }
                        else if (wagonFile.NumWagWheels >= 0f) { numAllAxles = (int)wagonFile.NumWagWheels; }
                        else { numAllAxles = 4; }
                        if (numDriveAxles > numAllAxles) { numAllAxles = numDriveAxles; }

                        // exclude legacy EOT from total axle count
                        if (!wag.IsEOT && wagonFile.WagonSize.LengthM > 1.1)
                        {
                            NumAxles += numAllAxles;
                        }

                        if (numAllAxles > 0 && wagonFile.MassKG > 1000)
                        {
                            const float GravitationalAccelerationMpS2 = 9.80665f;
                            var derailForce = wagonFile.MassKG / numAllAxles / 2f * GravitationalAccelerationMpS2;
                            if (derailForce > 1000f) { MinDerailForceN = Math.Min(MinDerailForceN, derailForce); }
                        }
                    }
                    catch (IOException e) // continue without details when eng/wag file does not exist
                    {
                        if (wag.IsEngine) { EngCount++; } else { WagCount++; }
                    }

                    if (!wag.IsEngine && EngCount > 0)
                    {
                        NumEngines = NumEngines + Separator + EngCount.ToString();
                        EngCount = 0; Separator = "+";
                    }

                    CarList.Add(new Car(wag, wagonMassKG));
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
            public readonly bool IsEngine;
            public readonly float MassKG;

            internal Car(Wagon car, float massKg)
            {
                ID = car.UiD.ToString();
                Name = car.Folder + "/" + car.Name;
                Direction = car.Flip ? Consist.Direction.Backwards : Consist.Direction.Forwards;
                IsEngine = car.IsEngine;
                MassKG = massKg;
            }
        }
    }
}
