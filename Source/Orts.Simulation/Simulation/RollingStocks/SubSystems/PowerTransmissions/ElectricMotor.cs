// COPYRIGHT 2011 by the Open Rails project.
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

using System;
using System.IO;
using ORTS.Common;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class ElectricMotor : ISubSystem<ElectricMotor>
    {
        protected float temperatureK;
        public float TemperatureK { get { return temperatureK; } }

        Integrator tempIntegrator = new Integrator();

        public float ThermalCoeffJ_m2sC;
        public float SpecificHeatCapacityJ_kg_C;
        public float SurfaceM;
        public float WeightKg;

        protected float powerLossesW;

        public float CoolingPowerW;

        public Axle AxleConnected;

        public readonly float InertiaKgm2;

        public readonly MSTSLocomotive Locomotive;

        public ElectricMotor(Axle axle, MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            InertiaKgm2 = 1.0f;
            temperatureK = 273.0f;
            ThermalCoeffJ_m2sC = 50.0f;
            SpecificHeatCapacityJ_kg_C = 40.0f;
            SurfaceM = 2.0f;
            WeightKg = 5.0f;
            AxleConnected = axle;
            AxleConnected.Motor = this;
            AxleConnected.TransmissionRatio = 1;
        }
        public virtual void UpdateTractiveForce(float elapsedClockSeconds, float t)
        {

        }

        public virtual double GetDevelopedTorqueNm(double motorSpeedRadpS)
        {
            return 0;
        }

        public virtual void Update(float timeSpan)
        {
            temperatureK = tempIntegrator.Integrate(timeSpan, (temperatureK) => 1.0f/(SpecificHeatCapacityJ_kg_C * WeightKg)*((powerLossesW - CoolingPowerW) / (ThermalCoeffJ_m2sC * SurfaceM) - temperatureK));
        }
        public virtual void Initialize()
        {
        }

        public virtual void InitializeMoving()
        {
        }
        public virtual void Copy(ElectricMotor other)
        {

        }
        public virtual void Save(BinaryWriter outf)
        {

        }
        public virtual void Restore(BinaryReader inf)
        {

        }
    }
}
