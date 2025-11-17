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


using Orts.Parsers.Msts;

namespace Orts.Common
{
    static class SteamTable
    {
        // gauge pressures that match other tables (pounds per square inch - Gauge pressure)
        static float[] PressureTableGaugePSI = new float[]
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

        // Specific heat table for water - volume heat capacity?? - 
        static float[] SpecificHeatTableKJpKGpK = new float[]
        {
            4.2170f, 4.2049f, 4.2165f, 4.2223f, 4.2287f, 4.2355f, 4.2427f, 4.2505f, 4.2587f, 4.2675f, 4.2769f,
            4.2926f, 4.3035f, 4.3151f, 4.3274f, 4.3405f, 4.3543f, 4.3690f, 4.3846f, 4.4012f, 4.4187f,
            4.4374f, 4.4573f, 4.4784f, 4.5009f, 4.5248f, 4.5503f, 4.5774f, 4.6064f, 4.6373f, 4.6703f
        };

        // Water temp in deg Kelvin
        static float[] WaterTemperatureTableK = new float[]
        {
            274.00f, 281.40f, 288.80f, 296.20f, 303.60f, 311.00f, 318.40f, 325.80f, 333.20f, 340.60f, 348.00f,
            355.40f, 362.80f, 370.20f, 377.60f, 385.00f, 392.40f, 399.80f, 407.20f, 414.60f, 422.00f,
            429.40f, 436.80f, 444.20f, 451.60f, 459.00f, 466.40f, 473.80f, 481.20f, 488.60f, 496.00f
        };

        static float[] SaturationPressureTablePSI = new float[]
        {
            0.00f, 10.00f, 20.00f, 30.00f, 40.00f, 50.00f, 60.00f, 70.00f, 80.00f, 90.00f, 100.00f,
            110.00f, 120.00f, 130.00f, 140.00f, 150.00f, 160.00f, 170.00f, 180.00f, 190.00f, 200.00f,
            210.00f, 220.00f, 230.00f, 240.00f, 250.00f, 260.00f, 270.00f, 280.00f, 290.00f, 300.00f
        };

        // Temperature of water in deg Kelvin
        static float[] TemperatureTableK = new float[]
        {
            372.76f, 388.11f, 398.94f, 407.45f, 414.53f, 420.62f, 426.01f, 430.84f, 435.23f, 439.27f, 443.02f,
            446.51f, 449.79f, 452.88f, 455.80f, 458.58f, 461.23f, 463.77f, 466.20f, 468.53f, 470.78f,
            472.94f, 475.03f, 477.06f, 479.02f, 480.92f, 482.76f, 484.56f, 486.30f, 488.00f, 489.66f
        };

        // Fire Rate - ie lbs of coal per Square Foot of Grate Area 
        static float[] CoalGrateAreaTableLbspFt2 = new float[]
        {
            0.0f, 20.0f, 40.0f, 60.0f, 80.0f, 100.0f, 120.0f, 140.0f, 160.0f, 180.0f, 200.0f, 220.0f
        };

        // Boiler Efficiency - based upon average results from test papers
        static float[] SatBoilerEfficiencyTableX = new float[]
        {
            0.80f, 0.749f, 0.69f, 0.63f, 0.571f, 0.512f, 0.452f, 0.393f, 0.334f, 0.274f, 0.215f, 0.156f
        };

        // Boiler Efficiency - based upon average results from test papers
        static float[] SuperBoilerEfficiencyTableX = new float[]
        {
            0.903f, 0.8484f, 0.7936f, 0.7390f, 0.6843f, 0.6296f, 0.5749f, 0.5202f, 0.4655f, 0.4108f, 0.3561f, 0.3014f
        };

        // Definition of Default Injector - Based upon actual tests on a live steam 10.5mm injector described in 1928 Sellers Injector Manual

        // Injector pressure for Sellers Injectors for water delivery
        static float[] InjectorBoilerPressureTable2PSI = new float[]
        {
            25.0f, 50.0f, 75.0f, 100.0f, 125.0f, 150.00f, 175.00f, 200.00f, 225.00f, 250.0f, 275.0f, 300.0f, 325.0f, 350.0f
        };

        // Injector feedwater temperature (F) for Sellers Injectors
        static float[] InjectorFeedwaterTemperatureTableF = new float[]
        {
            50f, 65f, 80f, 95f, 110f, 125f, 140f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 50F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater50FTable = new float[]
        {
            15189f, 19612f, 23284f, 26371f, 29209f, 31295f, 33382f, 34550f, 34967f, 34717f, 33882f, 32380f, 30210f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 65F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater65FTable = new float[]
        {
            14771f, 19361f, 22950f, 25954f, 28708f, 30878f, 32547f, 33715f, 33799f, 32881f, 31713f, 29459f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 80F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater80FTable = new float[]
        {
            14521f, 18944f, 22366f, 25370f, 28124f, 30924f, 31713f, 31796f, 30878f, 29376f, 26622f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 95F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater95FTable = new float[]
        {
            13937f, 18527f, 21782f, 25036f, 27289f, 28625f, 28708f, 27707f, 25287f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 110F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater110FTable = new float[]
        {
            13353f, 17943f, 21030f, 23617f, 25203f, 25543f, 23200f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 125F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater125FTable = new float[]
        {
            13102f, 17358f, 19528f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Maximum Injector Capacity @ 140F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMaxFeedwater140FTable = new float[]
        {
            12518f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 50F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater50FTable = new float[]
        {
            5341f, 6593f, 7594f, 85961f, 9764f, 10849f, 12184f, 13603f, 15356f, 17275f, 19528f, 22783f, 27289f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 65F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater65FTable = new float[]
        {
            5925f, 7344f, 8512f, 9681f, 10766f, 12101f, 13603f, 15272f, 17192f, 19528f, 22533f, 26371f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 80F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater80FTable = new float[]
        {
            6593f, 8345f, 9597f, 10766f, 12017f, 13520f, 15189f, 17192f, 19361f, 22533f, 26622f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 95F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater95FTable = new float[]
        {
            7344f, 8930f, 10348f, 11684f, 13269f, 15105f, 17108f, 19612f, 23951f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 110F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater110FTable = new float[]
        {
            8345f, 9848f, 11600f, 13436f, 15522f, 18193f, 23200f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 125F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater125FTable = new float[]
        {
            9764f, 11767f, 15689f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Tables for water delivered into the boiler (lbs) - Minimum Injector Capacity @ 140F - Ref 1928 Sellers Injector Manual - pg 87
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] WaterDeliveryMinFeedwater140FTable = new float[]
        {
            11350f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
        };

        // Pounds (lbs) of steam used per pound (lb) of water feed into boiler - Ref 1928 Sellers Injector Manual - pg 92
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        static float[] SteamLbsPerWaterLbs = new float[]
        {
            25.9f, 20.0f, 17.2f, 15.2f, 14.0f, 12.8f, 11.8f, 10.7f, 9.7f, 8.6f, 7.6f, 6.6f, 5.5f, 4.6f
        };

        // Injector delivery Temperature (F) varies with steam pressure (PSI) and water delivery (lbs) 
        // Ref 1928 Sellers Injector Manual - pg 92
        // https://babel.hathitrust.org/cgi/pt?id=coo.31924004617340&seq=91
        // and a level of extrapolation using Strickland Kneass formula - 


        // 25.0f, 50.0f, 75.0f, 100.0f, 125.0f, 150.00f, 175.00f, 200.00f, 225.00f, 250.0f, 275.0f, 300.0f, 325.0f, 350.0f

        static float[] WaterDelivery25PSITable = new float[]
        {
            5893f, 14691f
        };

        static float[] WaterDeliveryTemperatureF25PSITable = new float[]
       {
            154f, 103f
       };

        static float[] WaterDelivery50PSITable = new float[]
        {
            7304f, 19256f
        };

        static float[] WaterDeliveryTemperatureF50PSITable = new float[]
       {
            193f, 119f
       };

        static float[] WaterDelivery75PSITable = new float[]
        {
            8374f, 22883f
        };

        static float[] WaterDeliveryTemperatureF75PSITable = new float[]
       {
            215f, 128f
       };

        static float[] WaterDelivery100PSITable = new float[]
        {
            9628f, 25813f
        };

        static float[] WaterDeliveryTemperatureF100PSITable = new float[]
       {
            229f, 136f
       };

        static float[] WaterDelivery125PSITable = new float[]
        {
            10707f, 28552f
        };

        static float[] WaterDeliveryTemperatureF125PSITable = new float[]
       {
            242f, 143f
       };

        static float[] WaterDelivery150PSITable = new float[]
        {
            12201f, 30867f
        };

        static float[] WaterDeliveryTemperatureF150PSITable = new float[]
       {
            245f, 149f
       };

        static float[] WaterDelivery175PSITable = new float[]
        {
            13695f, 32702f
};

        static float[] WaterDeliveryTemperatureF175PSITable = new float[]
       {
            248f, 155.5f
       };

        static float[] WaterDelivery200PSITable = new float[]
        {
            15321f, 33764f
        };

        static float[] WaterDeliveryTemperatureF200PSITable = new float[]
       {
            249f, 164f
       };

        static float[] WaterDelivery225PSITable = new float[]
        {
            17197f, 33764f
        };

        static float[] WaterDeliveryTemperatureF225PSITable = new float[]
       {
            245f, 174f
       };

        static float[] WaterDelivery250PSITable = new float[]
        {
            19422f, 32702f
        };

        static float[] WaterDeliveryTemperatureF250PSITable = new float[]
       {
            239f, 186f
       };

        static float[] WaterDelivery275PSITable = new float[]
        {
            22410f, 31540f
        };

        static float[] WaterDeliveryTemperatureF275PSITable = new float[]
       {
           229f, 200f
       };

        static float[] WaterDelivery300PSITable = new float[]
        {
            26228f, 29299f
        };

        static float[] WaterDeliveryTemperatureF300PSITable = new float[]
       {
            214f, 218f
       };

        static float[] WaterDelivery325PSITable = new float[] // These values need to be confirmed
        {
            29389f, 26602f
        };

        static float[] WaterDeliveryTemperatureF325PSITable = new float[]
       {
            208f, 245f
       };

        static float[] WaterDelivery350PSITable = new float[]
        {
            33285f, 23199f
        };

        static float[] WaterDeliveryTemperatureF350PSITable = new float[]
       {
            198f, 281f
       };

        // Cylinder Indicator Card Events

        // cutoff fraction
        static float[] CutOffFractionEventTableX = new float[]
        {
           0.05f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f, 0.8f
        };


        // Indicator Event - Exhaust Open
        static float[] CylinderExhaustTableX = new float[]
        {
           0.5306f, 0.6122f, 0.6646f, 0.7042f, 0.7358f, 0.7628f, 0.7866f, 0.8076f, 0.8270f, 0.8451f, 0.8626f, 0.8792f, 0.8955f, 0.9112f, 0.9269f, 0.9422f
        };

        // Indicator Event - Compression Close
        static float[] CylinderCompressionTableX = new float[]
        {
           0.4580f, 0.3864f, 0.3418f, 0.3082f, 0.2811f, 0.2575f, 0.2363f, 0.2170f, 0.1988f, 0.1814f, 0.1641f, 0.1471f, 0.1299f, 0.1127f, 0.0950f, 0.0771f
        };

        // Indicator Event - Admission Open
        static float[] CylinderAdmissionTableX = new float[]
        {
           0.0241f, 0.0121f, 0.0080f, 0.0058f, 0.0046f, 0.0037f, 0.0030f, 0.0026f, 0.0022f, 0.0019f, 0.0015f, 0.0013f, 0.0011f, 0.0009f, 0.0008f, 0.0006f
        };


        // Cylinder condensation and superheat

        // cutoff fraction
        static float[] CutOffFractionTableX = new float[]
        {
           0.05f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f, 0.55f
        };

        // cylinder condensation fraction per cutoff fraction - saturated steam (upper and lower ends extrapolated) - Ref Elseco Superheater manual
        static float[] CylinderCondensationFractionTableX = new float[]
        {
            0.526f, 0.42f, 0.345f, 0.29f, 0.245f, 0.213f, 0.181f, 0.159f, 0.142f, 0.125f, 0.11f
        };

        // Superheat required to prevent cylinder condensation fraction per cutoff fraction (upper and lower ends extrapolated) - Ref Elseco Superheater Engineering Data manual
        static float[] SuperheatCondenstationLimitTableDegF = new float[]
        {
            265.0f, 245.0f, 223.0f, 190.0f, 166.0f, 145.0f, 128.0f, 110.0f, 100.0f, 75.0f, 60.0f
        };

        // Steam to Cylinders - lbs per sec - from BTC Test Results for Std 8
        static float[] CylinderSteamTableLbpH = new float[]
        {
            0.0f, 2000.0f, 4000.0f, 6000.0f, 8000.0f, 10000.0f, 12000.0f, 14000.0f, 16000.0f, 18000.0f, 20000.0f, 22000.0f, 24000.0f, 26000.0f, 28000.0f, 30000.0f,
            32000.0f, 34000.0f, 36000.0f
        };

        // Superheat Temp - deg F - from BTC Test Results for Std 8
        static float[] SuperheatTempTableDegF = new float[]
        {
            0.0f, 40.0f, 70.0f, 100.0f, 140.0f, 164.0f, 195.0f, 220.0f, 242.0f, 260.0f, 278.0f, 290.0f, 304.0f, 320.0f, 335.0f, 348.0f,
            360.0f, 375.0f, 384.0f
        };

        // Allowance for drop in initial pressure (steam chest) as speed increases - Various sources
        static float[] WheelRotationRpM = new float[]
        {
            0.0f, 50.0f, 100.0f, 150.0f, 200.0f, 250.0f, 300.0f, 350.0f, 400.0f, 450.0f, 500.0f, 550.0f, 600.0f, 650.0f, 700.0f, 750.0f
        };

        // Allowance for drop in initial pressure (steam chest) as speed increases - Various sources - Saturated
        static float[] SatInitialPressureDropRatio = new float[]
        {
            0.98f, 0.965f, 0.95f, 0.935f, 0.92f, 0.905f, 0.89f, 0.875f, 0.87f, 0.8650f, 0.8625f, 0.86f, 0.8575f, 0.855f, 0.8525f, 0.85f

        };

        // Allowance for pressure drop in Steam chest pressure compared to Boiler Pressure - (To be confirmed) - Superheated
        static float[] SuperInitialPressureDropRatio = new float[]
        {
            0.99f, 0.98f, 0.97f, 0.96f, 0.95f, 0.94f, 0.93f, 0.92f, 0.915f, 0.910f, 0.905f, 0.90f, 0.8975f, 0.8950f, 0.8925f, 0.8900f
        };

        // piston speed (feet per minute) - American Locomotive Company
        static float[] PistonSpeedFtpMin = new float[]
        {
              0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100
        };

        // Speed factor - Saturated (0 and 2000, 2100 value extrapolated for Open Rails to limit TE) - Based upon dat from American Locomotive Company
        static float[] SpeedFactorSat = new float[]
        {
             1.0f, 1.0f, 1.0f, 0.954f, 0.863f, 0.772f, 0.680f, 0.590f, 0.517f, 0.460f, 0.412f, 0.372f, 0.337f, 0.307f, 0.283f, 0.261f, 0.241f, 0.225f, 0.213f, 0.202f, 0.190f, 0.185f
        };

        // Speed factor - Superheated (0 and 2000, 2100 value extrapolated for Open Rails to limit TE) - American Locomotive Company
        static float[] SpeedFactorSuper = new float[]
        {
              1.0f, 1.0f, 1.0f, 0.988f, 0.965f, 0.912f, 0.859f, 0.800f, 0.753f, 0.706f, 0.659f, 0.612f, 0.571f, 0.535f, 0.500f, 0.471f, 0.447f, 0.433f, 0.424f, 0.420f, 0.410f, 0.410f
        };

        // Indicated HorsePower - 
        static float[] IndicatedHorsepowerIHP = new float[]
        {
              0.0f, 200.0f, 400.0f, 600.0f, 800.0f, 1000.0f, 1200.0f, 1400.0f, 1600.0f,
              1800.0f, 2000.0f, 2200.0f, 2400.0f, 2600.0f, 2800.0f, 3000.0f
        };

        // BackPressure - Saturated locomotive -  Ref Principles of Locomotive Operation - Assume atmospheric - extrapolated beyond 1800IHP
        static float[] BackPressureSatPSI = new float[]
        {
              0.0f, 1.0f, 2.0f, 2.33f, 3.0f, 5.3f, 5.6f, 8.0f, 11.2f,
              14.25f, 16.0f, 20.0f, 24.0f, 26.0f, 28.0f, 30.0f
        };

        // BackPressure - Superheated locomotive -  Ref Principles of Locomotive Operation - Assume atmospheric - extrapolated beyond 1800IHP
        static float[] BackPressureSuperPSI = new float[]
        {
              0.0f, 0.25f, 0.5f, 0.75f, 1.25f, 1.75f, 2.5f, 3.5f, 4.8f,
              7.2f, 11.25f, 16.0f, 20.0f, 22.0f, 24.0f, 26.0f
        };

        // Allowance for drop in initial pressure (steam chest) as speed increases - Various sources
        static float[] CondensationWheelRotationRpM = new float[]
        {
            0.0f, 50.0f, 100.0f, 150.0f, 200.0f, 250.0f, 300.0f, 350.0f
        };

        // Steam Tables

        // Indicator Diagram - Cylinder Events

        // Indicator Diagram Event - Exhaust Open - Perwall program - http://5at.co.uk/index.php/references-and-links/software.html
        public static Interpolator CylinderEventExhausttoCutoff()
        {
            return new Interpolator(CutOffFractionEventTableX, CylinderExhaustTableX);
        }

        // Indicator Diagram Event - Compression Open - Perwall program - http://5at.co.uk/index.php/references-and-links/software.html
        public static Interpolator CylinderEventCompressiontoCutoff()
        {
            return new Interpolator(CutOffFractionEventTableX, CylinderCompressionTableX);
        }

        // Indicator Diagram Event - Admission Open - Perwall program - http://5at.co.uk/index.php/references-and-links/software.html
        public static Interpolator CylinderEventAdmissiontoCutoff()
        {
            return new Interpolator(CutOffFractionEventTableX, CylinderAdmissionTableX);
        }

        // cylinder condensation fraction per cutoff fraction - saturated steam - Ref Elseco Superheater manual
        public static Interpolator CylinderCondensationFractionInterpolatorX()
        {
            return new Interpolator(CutOffFractionTableX, CylinderCondensationFractionTableX);
        }

        // Superheat temp required to prevent cylinder condensation - Ref Elseco Superheater manual
        public static Interpolator SuperheatTempLimitInterpolatorXtoDegF()
        {
            return new Interpolator(CutOffFractionTableX, SuperheatCondenstationLimitTableDegF);
        }

        // Saturated Backpressure - Ref Principles of Locomotive Operation
        public static Interpolator BackpressureSatIHPtoPSI()
        {
            return new Interpolator(IndicatedHorsepowerIHP, BackPressureSatPSI);
        }

        // Superheated Backpressure - Ref Principles of Locomotive Operation
        public static Interpolator BackpressureSuperIHPtoPSI()
        {
            return new Interpolator(IndicatedHorsepowerIHP, BackPressureSuperPSI);
        }


        // Saturated Speed factor - ie drop in TE as speed increases due to piston impacts - Ref American locomotive Company
        public static Interpolator SaturatedSpeedFactorSpeedDropFtpMintoX()
        {
            return new Interpolator(PistonSpeedFtpMin, SpeedFactorSat);
        }


        // Superheated Speed factor - ie drop in TE as speed increases due to piston impacts - Ref American locomotive Company
        public static Interpolator SuperheatedSpeedFactorSpeedDropFtpMintoX()
        {
            return new Interpolator(PistonSpeedFtpMin, SpeedFactorSuper);
        }


        // Allowance for pressure drop in Steam chest pressure compared to Boiler Pressure - Ref LOCOMOTIVE OPERATION - A TECHNICAL AND PRACTICAL ANALYSIS - BY G. R. HENDERSON
        public static Interpolator SuperInitialPressureDropRatioInterpolatorRpMtoX()
        {
            return new Interpolator(WheelRotationRpM, SuperInitialPressureDropRatio);
        }

        // Allowance for wire-drawing - ie drop in initial pressure (cutoff) as speed increases - Ref Principles of Locomotive Operation
        public static Interpolator SatInitialPressureDropRatioInterpolatorRpMtoX()
        {
            return new Interpolator(WheelRotationRpM, SatInitialPressureDropRatio);
        }

        // Superheat temp per lbs of steam to cylinder - from BTC Test Results for Std 8
        public static Interpolator SuperheatTempInterpolatorLbpHtoDegF()
        {
            return new Interpolator(CylinderSteamTableLbpH, SuperheatTempTableDegF);
        }

        // Definition of Default Injector - Based upon actual tests on a live steam 10.5mm injector described in 1928 Sellers Injector Manual

        // Build 2D Interpolator for Maximum water delivery per boiler steam pressure and feedwater temperature

        public static Interpolator WaterDeliveryMaximumLBperPSIat50F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater50FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat65F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater65FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat80F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater80FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat95F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater95FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat110F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater110FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat125F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater125FTable);
        }

        public static Interpolator WaterDeliveryMaximumLBperPSIat140F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMaxFeedwater140FTable);
        }

        // Build Combined Interpolator Array
        static Interpolator[] Initial_water_delivery_maxima = new Interpolator[]
        {
            WaterDeliveryMaximumLBperPSIat50F(),
            WaterDeliveryMaximumLBperPSIat65F(),
            WaterDeliveryMaximumLBperPSIat80F(),
            WaterDeliveryMaximumLBperPSIat95F(),
            WaterDeliveryMaximumLBperPSIat110F(),
            WaterDeliveryMaximumLBperPSIat125F(),
            WaterDeliveryMaximumLBperPSIat140F()
        };

        // Final call
        public static Interpolator2D WaterInjectorDeliveryMaximaLbsPerPSIPerF()
        {
            return new Interpolator2D(InjectorFeedwaterTemperatureTableF, Initial_water_delivery_maxima);
        }

        // Build 2D Interpolator for Maximum water delivery per boiler steam pressure and feedwater temperature

        public static Interpolator WaterDeliveryMinimumLBperPSIat50F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater50FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat65F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater65FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat80F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater80FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat95F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater95FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat110F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater110FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat125F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater125FTable);
        }

        public static Interpolator WaterDeliveryMinimumLBperPSIat140F()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, WaterDeliveryMinFeedwater140FTable);
        }

        // Build Combined Interpolator Array
        static Interpolator[] Initial_water_delivery_minima = new Interpolator[]
        {
            WaterDeliveryMinimumLBperPSIat50F(),
            WaterDeliveryMinimumLBperPSIat65F(),
            WaterDeliveryMinimumLBperPSIat80F(),
            WaterDeliveryMinimumLBperPSIat95F(),
            WaterDeliveryMinimumLBperPSIat110F(),
            WaterDeliveryMinimumLBperPSIat125F(),
            WaterDeliveryMinimumLBperPSIat140F()
        };

        // Final call
        public static Interpolator2D WaterInjectorDeliveryMinimaLbsPerPSIPerF()
        {
            return new Interpolator2D(InjectorFeedwaterTemperatureTableF, Initial_water_delivery_minima);
        }

        // Build 2D Interpolator for water delivery temperature per boiler steam pressure and water delivery

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat25PSI()
        {
            return new Interpolator(WaterDelivery25PSITable, WaterDeliveryTemperatureF25PSITable);
        }
        public static Interpolator WaterDeliveryTemperatureFperLBWaterat50PSI()
        {
            return new Interpolator(WaterDelivery50PSITable, WaterDeliveryTemperatureF50PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat75PSI()
        {
            return new Interpolator(WaterDelivery75PSITable, WaterDeliveryTemperatureF75PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat100PSI()
        {
            return new Interpolator(WaterDelivery100PSITable, WaterDeliveryTemperatureF100PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat125PSI()
        {
            return new Interpolator(WaterDelivery125PSITable, WaterDeliveryTemperatureF125PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat150PSI()
        {
            return new Interpolator(WaterDelivery150PSITable, WaterDeliveryTemperatureF150PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat175PSI()
        {
            return new Interpolator(WaterDelivery175PSITable, WaterDeliveryTemperatureF175PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat200PSI()
        {
            return new Interpolator(WaterDelivery200PSITable, WaterDeliveryTemperatureF200PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat225PSI()
        {
            return new Interpolator(WaterDelivery225PSITable, WaterDeliveryTemperatureF225PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat250PSI()
        {
            return new Interpolator(WaterDelivery250PSITable, WaterDeliveryTemperatureF250PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat275PSI()
        {
            return new Interpolator(WaterDelivery275PSITable, WaterDeliveryTemperatureF275PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat300PSI()
        {
            return new Interpolator(WaterDelivery300PSITable, WaterDeliveryTemperatureF300PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat325PSI()
        {
            return new Interpolator(WaterDelivery325PSITable, WaterDeliveryTemperatureF325PSITable);
        }

        public static Interpolator WaterDeliveryTemperatureFperLBWaterat350PSI()
        {
            return new Interpolator(WaterDelivery350PSITable, WaterDeliveryTemperatureF350PSITable);
        }

        // Build Combined Interpolator Array
        static Interpolator[] Initial_water_delivery_temperature = new Interpolator[]
        {
            WaterDeliveryTemperatureFperLBWaterat25PSI(),
            WaterDeliveryTemperatureFperLBWaterat50PSI(),
            WaterDeliveryTemperatureFperLBWaterat75PSI(),
            WaterDeliveryTemperatureFperLBWaterat100PSI(),
            WaterDeliveryTemperatureFperLBWaterat125PSI(),
            WaterDeliveryTemperatureFperLBWaterat150PSI(),
            WaterDeliveryTemperatureFperLBWaterat175PSI(),
            WaterDeliveryTemperatureFperLBWaterat200PSI(),
            WaterDeliveryTemperatureFperLBWaterat225PSI(),
            WaterDeliveryTemperatureFperLBWaterat250PSI(),
            WaterDeliveryTemperatureFperLBWaterat275PSI(),
            WaterDeliveryTemperatureFperLBWaterat300PSI(),
            WaterDeliveryTemperatureFperLBWaterat325PSI(),
            WaterDeliveryTemperatureFperLBWaterat325PSI()
        };

        // Final call
        public static Interpolator2D WaterInjectorDeliveryTemperatureFPerLbsPerPSI()
        {
            return new Interpolator2D(InjectorBoilerPressureTable2PSI, Initial_water_delivery_temperature);
        }

        // Injector steam (lbs) per pound of water @ boiler steam pressure (psi)
        public static Interpolator InjSteamUsedForWaterAtPressureInterpolatorLbstoPSI()
        {
            return new Interpolator(InjectorBoilerPressureTable2PSI, SteamLbsPerWaterLbs);
        }

        // Boiler Efficiency based on lbs of coal per sq. ft of Grate Area - Saturated
        public static Interpolator SatBoilerEfficiencyGrateAreaInterpolatorLbstoX()
        {
            return new Interpolator(CoalGrateAreaTableLbspFt2, SatBoilerEfficiencyTableX);
        }

        // Boiler Efficiency based on lbs of coal per sq. ft of Grate Area - Saturated
        public static Interpolator SuperBoilerEfficiencyGrateAreaInterpolatorLbstoX()
        {
            return new Interpolator(CoalGrateAreaTableLbspFt2, SuperBoilerEfficiencyTableX);
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

        public static Interpolator WaterHeatInterpolatorPSItoBTUpLB()
        {
            return new Interpolator(PressureTableGaugePSI, WaterHeatTableBTUpLB);
        }

        public static Interpolator WaterDensityInterpolatorPSItoLBpFT3()
        {
            return new Interpolator(PressureTableGaugePSI, WaterDensityTableLBpFT3);
        }

        public static Interpolator SteamHeatInterpolatorPSItoBTUpLB()
        {
            return new Interpolator(PressureTableGaugePSI, SteamHeatTableBTUpLB);
        }

        public static Interpolator SteamDensityInterpolatorPSItoLBpFT3()
        {
            return new Interpolator(PressureTableGaugePSI, SteamDensityTableLBpFT3);
        }

        public static Interpolator WaterHeatToPressureInterpolatorBTUpLBtoPSI()
        {
            return new Interpolator(WaterHeatTableBTUpLB, PressureTableGaugePSI);
        }

        public static Interpolator TemperatureToPressureInterpolatorFtoPSI()
        {
            return new Interpolator(TemperatureTableF, PressureTableGaugePSI);
        }

        public static Interpolator PressureToTemperatureInterpolatorPSItoF()
        {
            return new Interpolator(PressureTableGaugePSI, TemperatureTableF);
        }

        public static Interpolator SteamHeatPressureToTemperatureInterpolatorPSItoF()
        {
            return new Interpolator(PressureTableGaugePSI, TemperatureTableF);
        }

        // ++++++++++++++++++++++++++++++++
        // Interpolator2D for Cut-off pressure to Initial Pressure - Ref Plate 10 - LOCOMOTIVE OPERATION - A TECHNICAL AND PRACTICAL ANALYSIS - BY G. R. HENDERSON
        // 

        // revolutions - z value
        static float[] WheelRevolutionsRpM = new float[]
        {
            0.0f, 50.0f, 100.0f, 150.0f, 200.0f, 250.0f, 300.0f, 350.0f
        };

        // Cutoff - x Value
        static float[] CutOff = new float[]
        {
            0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f
        };

        // ++++++++++++++++++++++ Upper Limit ++++++++++++

        // % Initial Pressure @ 0rpm - y Value
        static float[] InitialPressureUpper0RpM = new float[]
        {
            1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f
        };

        // % Initial Pressure @ 50rpm - y Value
        static float[] InitialPressureUpper50RpM = new float[]
        {
            0.79f, 0.82f, 0.83f, 0.86f, 0.875f, 0.9f, 0.92f, 0.94f, 0.96f, 0.98f, 1.0f
        };

        // % Initial Pressure @ 100rpm - y Value
        static float[] InitialPressureUpper100RpM = new float[]
        {
            0.72f, 0.74f, 0.77f, 0.795f, 0.825f, 0.85f, 0.875f, 0.9f, 0.925f, 0.955f, 0.98f
        };

        // % Initial Pressure @ 150rpm - y Value
        static float[] InitialPressureUpper150RpM = new float[]
        {
              0.68f, 0.71f, 0.735f, 0.760f, 0.7875f, 0.8125f, 0.840f, 0.870f, 0.90f, 0.930f, 0.9575f
        };

        //  % Initial Pressure @ 200rpm - y Value
        static float[] InitialPressureUpper200RpM = new float[]
        {
              0.66f, 0.68f, 0.705f, 0.727f, 0.757f, 0.78f, 0.81f, 0.84f, 0.875f, 0.908f, 0.938f
        };

        //  % Initial Pressure @ 250rpm - y Value
        static float[] InitialPressureUpper250RpM = new float[]
        {
              0.645f, 0.665f, 0.6875f, 0.71f, 0.73f, 0.76f, 0.788f, 0.81f, 0.85f, 0.88f, 0.913f
        };

        //  % Initial Pressure @ 300rpm - y Value
        static float[] InitialPressureUpper300RpM = new float[]
        {
              0.63f, 0.6475f, 0.665f, 0.68f, 0.71f, 0.73f, 0.7625f, 0.79f, 0.81f, 0.85f, 0.877f
        };

        //  % Initial Pressure @ 350rpm - y Value
        static float[] InitialPressureUpper350RpM = new float[]
        {
              0.6125f, 0.63f, 0.65f, 0.663f, 0.6875f, 0.7125f, 0.7425f, 0.775f, 0.809f, 0.8375f, 0.865f
        };


        // Do intial Interpolation
        // 0RpM
        public static Interpolator CutOffInitialPressureUpper0RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper0RpM);
        }
        // 50RpM
        public static Interpolator CutOffInitialPressureUpper50RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper50RpM);
        }
        // 100RpM
        public static Interpolator CutOffInitialPressureUpper100RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper100RpM);
        }

        // 150RpM
        public static Interpolator CutOffInitialPressureUpper150RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper150RpM);
        }

        // 200RpM
        public static Interpolator CutOffInitialPressureUpper200RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper200RpM);
        }

        // 250RpM
        public static Interpolator CutOffInitialPressureUpper250RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper250RpM);
        }

        // 300RpM
        public static Interpolator CutOffInitialPressureUpper300RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper300RpM);
        }

        // 350RpM
        public static Interpolator CutOffInitialPressureUpper350RpM()
        {
            return new Interpolator(CutOff, InitialPressureUpper350RpM);
        }



        // Build Combined Interpolator Array
        static Interpolator[] Initial_pressure_upper = new Interpolator[]
        {
              CutOffInitialPressureUpper0RpM(),
              CutOffInitialPressureUpper50RpM(),
              CutOffInitialPressureUpper100RpM(),
              CutOffInitialPressureUpper150RpM(),
              CutOffInitialPressureUpper200RpM(),
              CutOffInitialPressureUpper250RpM(),
              CutOffInitialPressureUpper300RpM(),
              CutOffInitialPressureUpper350RpM(),
        };

        // Final call
        public static Interpolator2D CutoffInitialPressureUpper()
        {
            return new Interpolator2D(WheelRevolutionsRpM, Initial_pressure_upper);
        }

        // ++++++++++++++++++++++ Lower Limit ++++++++++++

        // % Initial Pressure @ 0rpm - y Value
        static float[] InitialPressureLower0RpM = new float[]
        {
            1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f
        };

        // % Initial Pressure @ 50rpm - y Value
        static float[] InitialPressureLower50RpM = new float[]
        {
              0.745f, 0.76f, 0.77f, 0.7875f, 0.8125f, 0.8375f, 0.865f, 0.9f, 0.93f, 0.97f, 1.0f
        };


        // % Initial Pressure @ 100rpm - y Value
        static float[] InitialPressureLower100RpM = new float[]
        {
              0.6875f, 0.6875f, 0.69f, 0.708f, 0.725f, 0.755f, 0.79f, 0.83f, 0.88f, 0.9325f, 0.9875f
        };

        // % Initial Pressure @ 150rpm - y Value
        static float[] InitialPressureLower150RpM = new float[]
        {
              0.64f, 0.64f, 0.645f, 0.6525f, 0.67f, 0.6975f, 0.731f, 0.775f, 0.825f, 0.875f, 0.93f
        };

        // % Initial Pressure @ 200rpm - y Value
        static float[] InitialPressureLower200RpM = new float[]
        {
              0.61f, 0.61f, 0.61f, 0.6125f, 0.625f, 0.65f, 0.68f, 0.72f, 0.775f, 0.83f, 0.88f
        };


        // % Initial Pressure @ 250rpm - y Value
        static float[] InitialPressureLower250RpM = new float[]
        {
              0.5775f,  0.5775f, 0.5775f, 0.585f, 0.595f, 0.62f, 0.653f, 0.69f, 0.7375f, 0.7825f, 0.825f
        };


        // % Initial Pressure @ 300rpm - y Value
        static float[] InitialPressureLower300RpM = new float[]
        {
              0.55f, 0.55f, 0.55f, 0.5575f, 0.57f, 0.59f, 0.62f, 0.6575f, 0.6925f, 0.73f, 0.7675f
        };

        // % Initial Pressure @ 350rpm - y Value
        static float[] InitialPressureLower350RpM = new float[]
        {
              0.52f, 0.52f, 0.52f, 0.528f, 0.545f, 0.57f, 0.60f, 0.635f, 0.67f, 0.7075f, 0.74f
        };


        // Do intial Interpolation
        // 0RpM
        public static Interpolator CutOffInitialPressureLower0RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower0RpM);
        }
        // 50RpM
        public static Interpolator CutOffInitialPressureLower50RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower50RpM);
        }
        // 100RpM
        public static Interpolator CutOffInitialPressureLower100RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower100RpM);
        }

        // 150RpM
        public static Interpolator CutOffInitialPressureLower150RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower150RpM);
        }

        // 200RpM
        public static Interpolator CutOffInitialPressureLower200RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower200RpM);
        }

        // 250RpM
        public static Interpolator CutOffInitialPressureLower250RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower250RpM);
        }

        // 300RpM
        public static Interpolator CutOffInitialPressureLower300RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower300RpM);
        }

        // 350RpM
        public static Interpolator CutOffInitialPressureLower350RpM()
        {
            return new Interpolator(CutOff, InitialPressureLower350RpM);
        }



        // Build Combined Interpolator Array
        static Interpolator[] Initial_pressure_lower = new Interpolator[]
        {
              CutOffInitialPressureLower0RpM(),
              CutOffInitialPressureLower50RpM(),
              CutOffInitialPressureLower100RpM(),
              CutOffInitialPressureLower150RpM(),
              CutOffInitialPressureLower200RpM(),
              CutOffInitialPressureLower250RpM(),
              CutOffInitialPressureLower300RpM(),
              CutOffInitialPressureLower350RpM(),
        };

        // Final call
        public static Interpolator2D CutoffInitialPressureLower()
        {
            return new Interpolator2D(WheelRevolutionsRpM, Initial_pressure_lower);
        }

    }
}
