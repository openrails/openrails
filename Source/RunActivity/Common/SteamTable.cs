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
    class SteamTable
    {
        // gauge pressures that match other tables (pounds per square inch)
        static float[] PressureTable = new float[]
        {
            0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
            110, 120, 130, 140, 150, 160, 170, 180, 190, 200,
            210, 220, 230, 240, 250, 260, 270, 280, 290, 300
        };
        // saturation temperature at various pressures (Fahrenheit)
        static float[] TemperatureTable = new float[]
        {
            212.0f, 239.4f, 258.7f, 274.0f, 286.7f, 297.7f, 307.3f, 316.0f, 323.9f, 331.2f, 337.9f,
            344.2f, 350.1f, 355.6f, 360.9f, 365.9f, 370.6f, 375.6f, 379.6f, 383.8f, 387.8f,
            391.7f, 395.5f, 399.1f, 402.6f, 406.0f, 409.4f, 412.6f, 415.7f, 418.8f, 421.8f
        };
        // total heat in water at various pressures (BTU per pound)
        static float[] WaterHeatTable = new float[]
        {
            180.15f, 207.83f, 227.51f, 243.08f, 256.09f, 267.34f, 277.31f, 286.30f, 294.49f, 302.05f, 309.08f,
            315.66f, 321.85f, 327.70f, 333.26f, 338.56f, 343.62f, 348.47f, 353.13f, 357.62f, 361.95f,
            366.14f, 370.19f, 374.12f, 377.94f, 381.65f, 385.26f, 388.78f, 392.21f, 395.56f, 398.84f
        };
        // density of water at various pressures (pounds per cubic foot)
        static float[] WaterDensityTable = new float[]
        {
            59.83f, 59.11f, 58.57f, 58.12f, 57.73f, 57.39f, 57.07f, 56.79f, 56.52f, 56.27f, 56.03f,
            55.81f, 55.59f, 55.39f, 55.19f, 55.00f, 54.82f, 54.65f, 54.48f, 54.31f, 54.15f,
            53.99f, 53.84f, 53.69f, 53.54f, 53.40f, 53.26f, 53.12f, 52.99f, 52.86f, 52.73f
        };
        // total heat in saturated steam at various pressures (BTU per pound)
        static float[] SteamHeatTable = new float[]
        {
            1150.28f, 1160.31f, 1167.01f, 1172.03f, 1176.00f, 1179.27f, 1182.04f, 1184.42f, 1186.50f, 1188.33f, 1189.95f,
            1191.41f, 1192.72f, 1193.91f, 1194.99f, 1195.97f, 1196.86f, 1197.68f, 1198.43f, 1199.12f, 1199.75f,
            1200.33f, 1200.86f, 1201.35f, 1201.79f, 1202.20f, 1202.58f, 1202.92f, 1203.23f, 1203.51f, 1203.77f
        };
        // density of saturated steam at various pressures (pounds per cubic foot)
        static float[] SteamDensityTable = new float[]
        {
            0.0373f, 0.0606f, 0.0834f, 0.1057f, 0.1277f, 0.1496f, 0.1713f, 0.1928f, 0.2143f, 0.2356f, 0.2569f,
            0.2782f, 0.2994f, 0.3205f, 0.3416f, 0.3627f, 0.3838f, 0.4048f, 0.4259f, 0.4469f, 0.4680f,
            0.4890f, 0.5101f, 0.5312f, 0.5522f, 0.5733f, 0.5944f, 0.6155f, 0.6367f, 0.6578f, 0.6790f
        };
        public static Interpolator WaterHeatInterpolator()
        {
            return new Interpolator(PressureTable, WaterHeatTable);
        }
        public static Interpolator WaterDensityInterpolator()
        {
            return new Interpolator(PressureTable, WaterDensityTable);
        }
        public static Interpolator SteamHeatInterpolator()
        {
            return new Interpolator(PressureTable, SteamHeatTable);
        }
        public static Interpolator SteamDensityInterpolator()
        {
            return new Interpolator(PressureTable, SteamDensityTable);
        }
        public static Interpolator WaterHeat2PressureInterpolator()
        {
            return new Interpolator(WaterHeatTable, PressureTable);
        }
        public static Interpolator Temperature2PressureInterpolator()
        {
            return new Interpolator(TemperatureTable, PressureTable);
        }
        public static Interpolator Pressure2TemperatureInterpolator()
        {
            return new Interpolator(PressureTable, TemperatureTable);
        }
    }
}
