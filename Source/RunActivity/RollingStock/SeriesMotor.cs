using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ORTS
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
                return armatureCurrentA * ArmatureResistanceOhms + backEMFvoltageV;
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

        float backEMFvoltageV;
        public float BackEMFvoltageV { get { return backEMFvoltageV; } }

        public float MotorConstant { set; get; }

        float fieldWb;

        ArrayList fieldTableCurrent;
        ArrayList fieldTableField;
        public float NominalRevolutionsRad;
        public float NominalVoltageV;
        public float NominalCurrentA;

        public float UpdateField()
        {
            float temp = 0.0f;
            if ((fieldTableCurrent == null) || (fieldTableField == null))
            {
                temp = (NominalVoltageV - (ArmatureResistanceOhms + FieldResistanceOhms) * NominalCurrentA) / (NominalRevolutionsRad);
                if (fieldCurrentA <= NominalCurrentA)
                    fieldWb = temp * fieldCurrentA / NominalCurrentA;
                else
                    fieldWb = temp;
            }
            else
                throw new NotImplementedException("Not implemented");
            temp *= (1.0f - shuntRatio);
            return temp;
        }


        public SeriesMotor()
        {
            NominalCurrentA = 1000.0f;
            NominalRevolutionsRad = 300.0f;
            NominalVoltageV = 1500.0f;
        }
        public SeriesMotor(float nomCurrentA, float nomVoltageV, float nomRevolutionsRad)
        {
            NominalCurrentA = nomCurrentA;
            NominalVoltageV = nomVoltageV;
            NominalRevolutionsRad = nomRevolutionsRad;
        }

        public override void Update(float timeSpan)
        {
            if (shuntResistorOhms == 0.0f)
                armatureCurrentA = fieldCurrentA / (1.0f - shuntRatio);
            else
                armatureCurrentA = (FieldResistanceOhms + ShuntResistorOhms) / ShuntResistorOhms * fieldCurrentA;
            if ((backEMFvoltageV * fieldCurrentA) >= 0.0f)
            {
                fieldCurrentA += timeSpan / FieldInductance *
                    (TerminalVoltageV
                        - backEMFvoltageV
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
            backEMFvoltageV = RevolutionsRad * fieldWb;
            developedTorqueNm = fieldWb * armatureCurrentA -(frictionTorqueNm * revolutionsRad /NominalRevolutionsRad * revolutionsRad/NominalRevolutionsRad);

            powerLossesW = ArmatureResistanceOhms * armatureCurrentA * armatureCurrentA +
                           FieldResistanceOhms * fieldCurrentA * fieldCurrentA;

            //temperatureK += timeSpan * ThermalCoeffJ_m2sC * SurfaceM / (SpecificHeatCapacityJ_kg_C * WeightKg)
            //    * ((powerLossesW - CoolingPowerKW) / (SpecificHeatCapacityJ_kg_C * WeightKg) - temperatureK);

            base.Update(timeSpan);
        }
        public override void Reset()
        {
            fieldCurrentA = 0.0f;
            armatureCurrentA = 0.0f;
            fieldWb = 0.0f;
            base.Reset();
        }
    }
}
