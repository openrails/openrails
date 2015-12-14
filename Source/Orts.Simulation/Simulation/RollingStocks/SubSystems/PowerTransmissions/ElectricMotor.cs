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

using ORTS.Common;
using System;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class ElectricMotor
    {
        protected float developedTorqueNm;
        public float DevelopedTorqueNm { get { return developedTorqueNm; } }

        protected float loadTorqueNm;
        public float LoadTorqueNm { set { loadTorqueNm = value; } get { return loadTorqueNm; } }

        protected float frictionTorqueNm;
        public float FrictionTorqueNm { set { frictionTorqueNm = Math.Abs(value); } get { return frictionTorqueNm; } }

        float inertiaKgm2;
        public float InertiaKgm2
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Inertia must be greater than 0");
                inertiaKgm2 = value;
            }
            get
            {
                return inertiaKgm2; 
            }
        }

        protected float revolutionsRad;
        public float RevolutionsRad { get { return revolutionsRad; } set { revolutionsRad = value; } }

        protected float temperatureK;
        public float TemperatureK { get { return temperatureK; } }

        Integrator tempIntegrator = new Integrator();

        public float ThermalCoeffJ_m2sC { set; get; }
        public float SpecificHeatCapacityJ_kg_C { set; get; }
        public float SurfaceM { set; get; }
        public float WeightKg { set; get; }

        protected float powerLossesW;

        public float CoolingPowerW { set; get; }

        float transmissionRatio;
        public float TransmissionRatio
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Transmission ratio must be greater than zero");
                transmissionRatio = value;
            }
            get
            {
                return transmissionRatio;
            }
        }

        float axleDiameterM;
        public float AxleDiameterM
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Axle diameter must be greater than zero");
                axleDiameterM = value;
            }
            get
            {
                return axleDiameterM;
            }
        }

        public Axle AxleConnected;

        public ElectricMotor()
        {
            developedTorqueNm = 0.0f;
            loadTorqueNm = 0.0f;
            inertiaKgm2 = 1.0f;
            revolutionsRad = 0.0f;
            axleDiameterM = 1.0f;
            transmissionRatio = 1.0f;
            temperatureK = 0.0f;
            ThermalCoeffJ_m2sC = 50.0f;
            SpecificHeatCapacityJ_kg_C = 40.0f;
            SurfaceM = 2.0f;
            WeightKg = 5.0f;
        }

        public virtual void Update(float timeSpan)
        {
            //revolutionsRad += timeSpan / inertiaKgm2 * (developedTorqueNm + loadTorqueNm + (revolutionsRad == 0.0 ? 0.0 : frictionTorqueNm));
            //if (revolutionsRad < 0.0)
            //    revolutionsRad = 0.0;
            temperatureK = tempIntegrator.Integrate(timeSpan, 1.0f/(SpecificHeatCapacityJ_kg_C * WeightKg)*((powerLossesW - CoolingPowerW) / (ThermalCoeffJ_m2sC * SurfaceM) - temperatureK));

        }

        public virtual void Reset()
        {
            revolutionsRad = 0.0f;
        }
    }
}
