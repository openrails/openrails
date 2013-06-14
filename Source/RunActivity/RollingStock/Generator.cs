using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS
{
    public class Generator
    {
        public enum GeneratorType
        {
            NotSet = 0,
            CurrentControlled = 1,
            VoltageControlled = 2
        }

        public Generator()
        {
            initialized = false;
            Type = GeneratorType.NotSet;
        }

        public Generator(Generator copy)
        {
            
            MaximalPowerW = copy.MaximalPowerW;
            maximalCurrentA = copy.MaximalCurrentA;
            MaximalVoltageV = copy.MaximalVoltageV;
            Type = copy.Type;
            initialized = copy.initialized;
            LoadTable = new Interpolator2D(copy.LoadTable);
            GeneratorPowerTab = new Interpolator(copy.GeneratorPowerTab);
            Type = copy.Type;
        }

        public GeneratorType Type;

        public bool initialized = false;

        float realRPM;
        public float RealRPM
        {
            get { return realRPM; }
            set
            {
                realRPM = value;
            }
        }

        public float ZeroLoadVoltageV { get { return LoadTable.Get(RealRPM, 0); } }

        public float InputPowerW { get { return OutputPowerW / EfficiencyPercent * 100f; } }

        public float DemandedPowerW;
        public float MaxInputPowerW;
        public float ExcitationPercet;

        public float DemandedCurrentA;
        public float DemandedVoltageV;

        float outputCurrentA;
        public float OutputCurrentA { get { return outputCurrentA; } }
        
        float outputVoltageV;
        public float OutputVoltageV
        {
            get
            {
                //return outputVoltageV; 
                return ZeroLoadVoltageV;
            }
        }
        public float OutputPowerW 
        {
            get 
            {
                return outputVoltageV * outputCurrentA;
            }
        }
        public float EfficiencyPercent;

        public float MaximalPowerW;
        float maxPowerW = 0;
        public float MaximalVoltageV;
        float maximalCurrentA;
        public float MaximalCurrentA
        {
            get
            {
                return maximalCurrentA;
            }
        }



        public Interpolator2D LoadTable = null;
        public Interpolator GeneratorPowerTab = null;

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(orts(generator(maximalpower": MaximalPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, null); initialized = true; break;
                case "engine(orts(generator(maximalvoltagev": MaximalVoltageV = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(orts(generator(maximalcurrenta": maximalCurrentA = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(orts(generator(efficiency": EfficiencyPercent = stf.ReadFloatBlock(STFReader.UNITS.None, 100); break;
                case "engine(orts(generator(loadtable": LoadTable = new Interpolator2D(stf); break;
                case "engine(orts(generator(generatorpowertab": GeneratorPowerTab = new Interpolator(stf); break;
                default: break;
            }
        }

        public void Update(float elapsedClockSeconds)
        {
            switch(Type)
            {
                case GeneratorType.CurrentControlled:
                    break;
                case GeneratorType.VoltageControlled:
                    break;
                case GeneratorType.NotSet:
                default:
                    break;
            }

            
            if (GeneratorPowerTab != null)
            {
                maxPowerW = GeneratorPowerTab[realRPM];
            }

            float power = Math.Min(maxPowerW, DemandedPowerW);

            if (outputVoltageV > 0)
                outputCurrentA = power / outputVoltageV;
            else
                outputCurrentA = 0;

            if (LoadTable != null)
            {
                outputVoltageV = LoadTable.Get(realRPM, OutputCurrentA);
            }

            return;
        }

        public string GetStatus()
        {
            var result = new StringBuilder();
            result.AppendFormat("Gen power = {0:F0} / {1:F0} kW\n", OutputPowerW / 1000f, maxPowerW );
            if (LoadTable != null)
            {
                result.AppendFormat("Gen voltage = {0:F0} V\n", OutputVoltageV);
                result.AppendFormat("Gen current r/d  = {0:F0} / {1:F0} A\n", OutputCurrentA, DemandedCurrentA);
                result.AppendFormat("Gen current max = {0:F0} A\n", MaximalCurrentA);
            }
            return result.ToString();
        }
    }
}
