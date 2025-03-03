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


namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class SeriesMotor : ElectricMotor
    {
        float armatureResistanceOhms;
        public float ArmatureResistanceOhms 
        {
            set
            {
                armatureResistanceOhms = value;
            }
            get
            {
                return armatureResistanceOhms * (235.0f + temperatureK) / (235.0f + 20.0f);
            }
        }
        public float ArmatureInductanceH { set; get; }

        float fieldResistanceOhms;
        public float FieldResistanceOhms
        {
            set
            {
                fieldResistanceOhms = value;
            }
            get
            {
                return fieldResistanceOhms * (235.0f + temperatureK) / (235.0f + 20.0f);
            }
        }
        public float FieldInductance { set; get; }

        public bool Compensated { set; get; }

        float armatureCurrentA;
        public float ArmatureCurrentA { get { return armatureCurrentA; } }

        float fieldCurrentA;
        public float FieldCurrentA { get { return fieldCurrentA; } }


        public float TerminalVoltageV { set; get; }

        public float ArmatureVoltageV
        {
            get
            {
                return armatureCurrentA * ArmatureResistanceOhms + BackEMFvoltageV;
            }
        }

        public float StartingResistorOhms { set; get; }
        public float AdditionalResistanceOhms { set; get; }

        float shuntResistorOhms;
        public float ShuntResistorOhms
        {
            set
            {
                if (value == 0.0f)
                {
                    shuntRatio = 0.0f;
                    shuntResistorOhms = 0.0f;
                }
                else
                    shuntResistorOhms = value;
            }
            get
            {
                if (shuntResistorOhms == 0.0f)
                    return float.PositiveInfinity;
                else
                    return shuntResistorOhms;
            }
        }

        float shuntRatio;
        public float ShuntPercent
        {
            set
            {
                shuntRatio = value / 100.0f;
            }
            get
            {
                if (shuntResistorOhms == 0.0f)
                    return shuntRatio * 100.0f;
                else
                    return 1.0f - shuntResistorOhms / (FieldResistanceOhms + shuntResistorOhms);
            }
        }
        public float BackEMFvoltageV { get; private set; }

        public float MotorConstant { set; get; }

        float fieldWb;

        public float NominalRevolutionsRad;
        public float NominalVoltageV;
        public float NominalCurrentA;

        public float UpdateField()
        {
            float temp = 0.0f;
            temp = (NominalVoltageV - (ArmatureResistanceOhms + FieldResistanceOhms) * NominalCurrentA) / (NominalRevolutionsRad);
            if (fieldCurrentA <= NominalCurrentA)
                fieldWb = temp * fieldCurrentA / NominalCurrentA;
            else
                fieldWb = temp;
            temp *= (1.0f - shuntRatio);
            return temp;
        }

        public SeriesMotor(float nomCurrentA, float nomVoltageV, float nomRevolutionsRad, Axle axle, MSTSLocomotive locomotive) : base(axle, locomotive)
        {
            NominalCurrentA = nomCurrentA;
            NominalVoltageV = nomVoltageV;
            NominalRevolutionsRad = nomRevolutionsRad;
        }

        public override double GetDevelopedTorqueNm(double revolutionsRad)
        {
            BackEMFvoltageV = (float)revolutionsRad * fieldWb;
            return fieldWb * armatureCurrentA/* - (frictionTorqueNm * revolutionsRad / NominalRevolutionsRad * revolutionsRad / NominalRevolutionsRad)*/;
        }

        public override void Update(float timeSpan)
        {
            if (shuntResistorOhms == 0.0f)
                armatureCurrentA = fieldCurrentA / (1.0f - shuntRatio);
            else
                armatureCurrentA = (FieldResistanceOhms + ShuntResistorOhms) / ShuntResistorOhms * fieldCurrentA;
            if ((BackEMFvoltageV * fieldCurrentA) >= 0.0f)
            {
                fieldCurrentA += timeSpan / FieldInductance *
                    (TerminalVoltageV
                        - BackEMFvoltageV
                        - ArmatureResistanceOhms * armatureCurrentA
                        - FieldResistanceOhms * (1.0f - shuntRatio) * fieldCurrentA
                        - ArmatureCurrentA * StartingResistorOhms
                        - ArmatureCurrentA * AdditionalResistanceOhms
                    //- ((fieldCurrentA == 0.0) ? 0.0 : 2.0)            //voltage drop on brushes
                    );
            }
            else
            {
                fieldCurrentA = 0.0f;
            }          

            UpdateField();

            powerLossesW = ArmatureResistanceOhms * armatureCurrentA * armatureCurrentA +
                           FieldResistanceOhms * fieldCurrentA * fieldCurrentA;

            //temperatureK += timeSpan * ThermalCoeffJ_m2sC * SurfaceM / (SpecificHeatCapacityJ_kg_C * WeightKg)
            //    * ((powerLossesW - CoolingPowerKW) / (SpecificHeatCapacityJ_kg_C * WeightKg) - temperatureK);

            base.Update(timeSpan);
        }
        public override void Initialize()
        {
            fieldCurrentA = 0.0f;
            armatureCurrentA = 0.0f;
            fieldWb = 0.0f;
            base.Initialize();
        }
    }
}
