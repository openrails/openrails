// COPYRIGHT 2010, 2011 by the Open Rails project.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    static class SteamTable
    {
        // gauge pressures that match other tables (pounds per square inch)
        static float[] PressureTablePSI = new float[]
        {
            0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
            110, 120, 130, 140, 150, 160, 170, 180, 190, 200,
            210, 220, 230, 240, 250, 260, 270, 280, 290, 300
        };
        // saturation temperature at various pressures (Fahrenheit)
        static float[] TemperatureTableF = new float[]
        {
            212.0f, 239.4f, 258.7f, 274.0f, 286.7f, 297.7f, 307.3f, 316.0f, 323.9f, 331.2f, 337.9f,
            344.2f, 350.1f, 355.6f, 360.9f, 365.9f, 370.6f, 375.6f, 379.6f, 383.8f, 387.8f,
            391.7f, 395.5f, 399.1f, 402.6f, 406.0f, 409.4f, 412.6f, 415.7f, 418.8f, 421.8f
        };
        // total heat in water at various pressures (BTU per pound)
        static float[] WaterHeatTableBTUpLB = new float[]
        {
            180.15f, 207.83f, 227.51f, 243.08f, 256.09f, 267.34f, 277.31f, 286.30f, 294.49f, 302.05f, 309.08f,
            315.66f, 321.85f, 327.70f, 333.26f, 338.56f, 343.62f, 348.47f, 353.13f, 357.62f, 361.95f,
            366.14f, 370.19f, 374.12f, 377.94f, 381.65f, 385.26f, 388.78f, 392.21f, 395.56f, 398.84f
        };
        // density of water at various pressures (pounds per cubic foot)
        static float[] WaterDensityTableLBpFT3 = new float[]
        {
            59.83f, 59.11f, 58.57f, 58.12f, 57.73f, 57.39f, 57.07f, 56.79f, 56.52f, 56.27f, 56.03f,
            55.81f, 55.59f, 55.39f, 55.19f, 55.00f, 54.82f, 54.65f, 54.48f, 54.31f, 54.15f,
            53.99f, 53.84f, 53.69f, 53.54f, 53.40f, 53.26f, 53.12f, 52.99f, 52.86f, 52.73f
        };
        // total heat in saturated steam at various pressures (BTU per pound)
        static float[] SteamHeatTableBTUpLB = new float[]
        {
            1150.28f, 1160.31f, 1167.01f, 1172.03f, 1176.00f, 1179.27f, 1182.04f, 1184.42f, 1186.50f, 1188.33f, 1189.95f,
            1191.41f, 1192.72f, 1193.91f, 1194.99f, 1195.97f, 1196.86f, 1197.68f, 1198.43f, 1199.12f, 1199.75f,
            1200.33f, 1200.86f, 1201.35f, 1201.79f, 1202.20f, 1202.58f, 1202.92f, 1203.23f, 1203.51f, 1203.77f
        };
        // density of saturated steam at various pressures (pounds per cubic foot)
        static float[] SteamDensityTableLBpFT3 = new float[]
        {
            0.0373f, 0.0606f, 0.0834f, 0.1057f, 0.1277f, 0.1496f, 0.1713f, 0.1928f, 0.2143f, 0.2356f, 0.2569f,
            0.2782f, 0.2994f, 0.3205f, 0.3416f, 0.3627f, 0.3838f, 0.4048f, 0.4259f, 0.4469f, 0.4680f,
            0.4890f, 0.5101f, 0.5312f, 0.5522f, 0.5733f, 0.5944f, 0.6155f, 0.6367f, 0.6578f, 0.6790f
        };
        // injector 9mm flowrates (gallons (uk) per minute) - data extrapolated below 40psi and over 180psi
        static float[] Injector09FlowTableUKGpM = new float[]
        {
            7.3f, 9.3f, 11.3f, 13.3f, 15.3f, 17.3f, 18.8f, 20.3f, 21.7f, 23.0f, 24.3f,
            25.45f, 26.6f, 27.65f, 28.7f, 29.7f, 30.7f, 31.7f, 32.7f, 33.7f, 34.7f,
            35.7f, 36.7f, 38.7f, 39.7f, 40.7f, 41.7f, 42.7f, 43.7f, 44.7f, 45.7f
        };
        // injector 10mm flowrates (gallons (uk) per minute) - data extrapolated below 40psi and over 200psi
        static float[] Injector10FlowTableUKGpM = new float[]
        {
            9.3f, 11.5f, 14.00f, 16.4f, 18.9f, 21.2f, 23.2f, 25.1f, 26.8f, 28.4f, 30.0f,
            31.2f, 32.3f, 33.45f, 35.4f, 36.65f, 37.9f, 39.05f, 40.2f, 41.2f, 42.4f,
            44.6f, 45.8f, 47.0f, 48.2f, 49.4f, 50.6f, 51.8f, 53.0f, 54.2f, 56.4f
        };
        // injector 11mm flowrates (gallons (uk) per minute) - data extrapolated below 40psi and over 200psi
        static float[] Injector11FlowTableUKGpM = new float[]
        {
            9.3f, 11.5f, 14.00f, 16.4f, 25.6f, 27.4f, 29.1f, 31.6f, 34.5f, 37.3f, 40.8f,
            42.0f, 43.2f, 44.45f, 45.7f, 46.5f, 47.3f, 48.5f, 49.7f, 52.6f, 55.5f,
            56.5f, 57.5f, 58.5f, 59.5f, 60.5f, 61.5f, 62.5f, 63.5f, 64.5f, 65.5f
        };
        // injector 13mm flowrates (gallons (uk) per minute) - data extrapolated below 40psi and over 200psi
        static float[] Injector13FlowTableUKGpM = new float[]
        {
            9.3f, 11.5f, 14.00f, 16.4f, 39.2f, 41.8f, 45.2f, 48.7f, 52.1f, 55.3f, 58.7f,
            60.65f, 62.6f, 64.4f, 66.2f, 67.95f, 69.7f, 72.9f, 76.1f, 78.05f, 80.0f,
            81.0f, 82.0f, 83.0f, 84.0f, 85.0f, 86.0f, 87.0f, 88.0f, 89.0f, 90.0f
        };
       
        // Temperature of water in deg Kelvin
                static float[] TemperatureTableK = new float[]
        {
            372.76f, 388.11f, 398.94f, 407.45f, 414.53f, 420.62f, 426.01f, 430.84f, 435.23f, 439.27f, 443.02f,
            446.51f, 449.79f, 452.88f, 455.80f, 458.58f, 461.23f, 463.77f, 466.20f, 468.53f, 470.78f, 
            472.94f, 475.03f, 477.06f, 479.02f, 480.92f, 482.76f, 484.56f, 486.30f, 488.00f, 489.66f, 
        };

        // Specific heat table for water - volume heat capacity?? - 
        static float[] SpecificHeatTableKJpKGpK = new float[]
        {
            4.2170f, 4.2049f, 4.2165f, 4.2223f, 4.2287f, 4.2355f, 4.2427f, 4.2505f, 4.2587f, 4.2675f, 4.2769f,
            4.2926f, 4.3035f, 4.3151f, 4.3274f, 4.3405f, 4.3543f, 4.3690f, 4.3846f, 4.4012f, 4.4187f, 
            4.4374f, 4.4573f, 4.4784f, 4.5009f, 4.5248f, 4.5503f, 4.5774f, 4.6064f, 4.6373f, 4.6703f, 
        };

        // Water temp in deg Kelvin
        static float[] WaterTemperatureTableK = new float[]
        {
            274.00f, 281.40f, 288.80f, 296.20f, 303.60f, 311.00f, 318.40f, 325.80f, 333.20f, 340.60f, 348.00f,
            355.40f, 362.80f, 370.20f, 377.60f, 385.00f, 392.40f, 399.80f, 407.20f, 414.60f, 422.00f, 
            429.40f, 436.80f, 444.20f, 451.60f, 459.00f, 466.40f, 473.80f, 481.20f, 488.60f, 496.00f, 
        };
       
        static float[] SaturationPressureTablePSI = new float[]
        {
            0.00f, 10.00f, 20.00f, 30.00f, 40.00f, 50.00f, 60.00f, 70.00f, 80.00f, 90.00f, 100.00f,
            110.00f, 120.00f, 130.00f, 140.00f, 150.00f, 160.00f, 170.00f, 180.00f, 190.00f, 200.00f, 
            210.00f, 220.00f, 230.00f, 240.00f, 250.00f, 260.00f, 270.00f, 280.00f, 290.00f, 300.00f, 
        };
        
        // pressure tables for Superheat Factors
        static float[] SuperheatFactorPressureTablePSI = new float[]
        {
            10.0f, 120.00f, 140.00f, 160.00f, 180.00f, 200.00f, 220.00f, 240.00f, 250.0f, 300.0f
        };

        // Table to calculate steam reduction due to superheating - based on figures from Goss book and test on superheating
        // Values represent fraction of steam usage compared to a saturated locomotive, ie 
        static float[] SuperheaterSteamReductionTable = new float[]
        {
            0.710f, 0.817f, 0.826f, 0.838f, 0.842f, 0.847f, 0.868f, 0.915f, 0.930f, 1.0f
        };
        
         // Table to calculate coal reduction due to superheating - based on figures from Goss book and test on superheating
        // Values represent fraction of steam usage compared to a saturated locomotive, ie 
        static float[] SuperheaterCoalReductionTable = new float[]
        {
            0.710f, 0.827f, 0.841f, 0.858f, 0.860f, 0.866f, 0.890f, 0.942f, 0.956f, 1.0f
        };
        
        // Fire Rate - ie lbs of coal per Square Foot of Grate Area 
        static float[] CoalGrateAreaTableLbspFt2 = new float[]
        {
            20.0f, 40.0f, 60.0f, 80.0f, 100.0f, 120.0f, 140.0f, 160.0f,
        };
        
        // Boiler Efficiency - based upon paper from Locomotive Stoker paper - values above 120, extrapolated
        static float[] BoilerEfficiencyTableX = new float[]
        {
            0.85f, 0.775f, 0.70f, 0.625f, 0.55f, 0.475f, 0.40f, 0.325f,
        };
        
        // pressure tables for Injectors temperature and steam usage
        static float[] InjectorUsePressureTablePSI = new float[]
        {
            75.0f, 150.00f, 175.00f, 200.00f, 225.00f
        };

        // Temperature tables for water delivered into the boiler (Fahrenheit) - Minimum Injector Capacity - Ref Sellers Injector
        static float[] WaterTemPDeliveryMinTableF = new float[]
        {
            217.0f, 255.00f, 257.00f, 260.00f, 269.00f
        };

        // Temperature tables for water delivered into the boiler (Fahrenheit) - Maximum Injector Capacity - Ref Sellers Injector
        static float[] WaterTemPDeliveryMaxTableF = new float[]
        {
            128.0f, 149.00f, 156.00f, 164.00f, 174.00f
        };

        // Water per fed through injector per pound of steam used - Ref Sellers Injector
        static float[] WaterDelFedSteamTableLbs = new float[]
        {
            17.20f, 12.80f, 11.80f, 10.70f, 9.70f
        };

        // Water per fed through injector factor to determine min capacity ie min/max - Ref Sellers Injector
        static float[] InjMinCapFactorTableX = new float[]
        {
            0.366f, 0.395f, 0.419f, 0.454f, 0.509f
        };

        // Injector factor to determine the min capacity of the injector
        public static Interpolator InjCapMinFactorInterpolatorX()
        {
            return new Interpolator(InjectorUsePressureTablePSI, InjMinCapFactorTableX);
        }       

        // Injector max delivery water temp (Fahr) per pressure of steam (psi)
        public static Interpolator InjDelWaterTempMaxPressureInterpolatorFtoPSI()
        {
            return new Interpolator(InjectorUsePressureTablePSI, WaterTemPDeliveryMaxTableF);
        }        
        
        // Injector min delivery water temp (Fahr) per pressure of steam (psi)
        public static Interpolator InjDelWaterTempMinPressureInterpolatorFtoPSI()
        {
            return new Interpolator(InjectorUsePressureTablePSI, WaterTemPDeliveryMinTableF);
        }  
        
        // Injector water fed per lb of steam at pressure of steam (psi)
        public static Interpolator InjWaterFedSteamPressureInterpolatorFtoPSI()
        {
            return new Interpolator(InjectorUsePressureTablePSI, WaterDelFedSteamTableLbs);
        }    

        // Boiler Efficiency based on lbs of coal per sq. ft of Grate Area
        public static Interpolator BoilerEfficiencyGrateAreaInterpolatorLbstoX()
        {
            return new Interpolator(CoalGrateAreaTableLbspFt2, BoilerEfficiencyTableX);
        }

        // Reduction of Steam usage at various steam pressures due to superheating
        public static Interpolator SuperheaterSteamReductionInterpolatorPSItoX()
        {
            return new Interpolator(SuperheatFactorPressureTablePSI, SuperheaterSteamReductionTable);
        }
        
        // Reduction of coal usage at various steam pressures due to superheating
        public static Interpolator SuperheaterCoalReductionInterpolatorPSItoX()
        {
            return new Interpolator(SuperheatFactorPressureTablePSI, SuperheaterCoalReductionTable);
        }
        

        // Saturated pressure of steam (psi) @ water temperature (K)
        public static Interpolator SaturationPressureInterpolatorKtoPSI()
        {
            return new Interpolator(TemperatureTableK, SaturationPressureTablePSI);
        }

        // table for specific heat capacity of water at temp of water
        public static Interpolator SpecificHeatInterpolatorKtoKJpKGpK()
        {
            return new Interpolator(WaterTemperatureTableK, SpecificHeatTableKJpKGpK);
        }

        // Flowrate table vs Boiler Pressure for 9mm Injector
        public static Interpolator Injector09FlowrateInterpolatorPSItoUKGpM()
        {
            return new Interpolator(PressureTablePSI, Injector09FlowTableUKGpM);
        }

        // Flowrate table vs Boiler Pressure for 10mm Injector
        public static Interpolator Injector10FlowrateInterpolatorPSItoUKGpM()
        {
            return new Interpolator(PressureTablePSI, Injector10FlowTableUKGpM);
        }

        // Flowrate table vs Boiler Pressure for 11mm Injector
        public static Interpolator Injector11FlowrateInterpolatorPSItoUKGpM()
        {
            return new Interpolator(PressureTablePSI, Injector11FlowTableUKGpM);
        } 
 
        // Flowrate table vs Boiler Pressure for 13mm Injector
        public static Interpolator Injector13FlowrateInterpolatorPSItoUKGpM()
        {
            return new Interpolator(PressureTablePSI, Injector13FlowTableUKGpM);
        } 

        public static Interpolator WaterHeatInterpolatorPSItoBTUpLB()
        {
            return new Interpolator(PressureTablePSI, WaterHeatTableBTUpLB);
        }

        public static Interpolator WaterDensityInterpolatorPSItoLBpFT3()
        {
            return new Interpolator(PressureTablePSI, WaterDensityTableLBpFT3);
        }

        public static Interpolator SteamHeatInterpolatorPSItoBTUpLB()
        {
            return new Interpolator(PressureTablePSI, SteamHeatTableBTUpLB);
        }

        public static Interpolator SteamDensityInterpolatorPSItoLBpFT3()
        {
            return new Interpolator(PressureTablePSI, SteamDensityTableLBpFT3);
        }

        public static Interpolator WaterHeatToPressureInterpolatorBTUpLBtoPSI()
        {
            return new Interpolator(WaterHeatTableBTUpLB, PressureTablePSI);
        }

        public static Interpolator TemperatureToPressureInterpolatorFtoPSI()
        {
            return new Interpolator(TemperatureTableF, PressureTablePSI);
        }

        public static Interpolator PressureToTemperatureInterpolatorPSItoF()
        {
            return new Interpolator(PressureTablePSI, TemperatureTableF);
        }
    }
}
