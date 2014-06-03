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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace LibAE
{
    public class AreaRoute
    {
        private float minX;
        private float minY;

        public float maxX { get; set; }
        public float maxY { get; set; }
        public int tileMinX { get; set; }
        public int tileMinZ { get; set; }
        public int tileMaxX { get; set; }
        public int tileMaxZ { get; set; }
        public List<TilesInfo> tilesList { get; set; }


        public AreaRoute()
        {
            tilesList = new List<TilesInfo>();

            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;
            tileMinX = int.MaxValue;
            tileMinZ = int.MaxValue;
            tileMaxX = int.MinValue;
            tileMaxZ = int.MinValue;
        }

        public float getMinY() { return minY; }
        public void setMinY(float y) { minY = y; }
        public float getMinX() { return minX; }
        public void setMinX(float x) { minX = x; }

        public void manageTiles(int TileX, int TileZ)
        {
            var selectedTile = from f in tilesList where f.TileX == TileX && f.TileZ == TileZ select f;
            if (selectedTile.Count() == 0)
                tilesList.Add(new TilesInfo(this, TileX, TileZ));
            if (TileX < tileMinX) tileMinX = TileX;
            if (TileZ < tileMinZ) tileMinZ = TileZ;
            if (TileX >= tileMaxX) tileMaxX = TileX;
            if (TileZ >= tileMaxZ) tileMaxZ = TileZ;
        }

    }

    public class TilesInfo
    {
        public float TileX;
        public float TileZ;

        public TilesInfo(AreaRoute areaRoute, float x, float z)
        {
            TileX = x;
            TileZ = z;
            areaRoute.maxX = Utility.CalcBounds(areaRoute.maxX, ((x + 1f) * 2048f) + 1024f, true);
            areaRoute.maxY = Utility.CalcBounds(areaRoute.maxY, ((z + 1f) * 2048f) + 1024f, true);

            areaRoute.setMinX(Utility.CalcBounds(areaRoute.getMinX(), (x * 2048f) -1024f, false));
            areaRoute.setMinY(Utility.CalcBounds(areaRoute.getMinY(), (z * 2048f) -1024f, false));
        }
    }

    public static class Utility
    {
        
        /// <summary>
        /// Given a value representing a limit, evaluate if the given value exceeds the current limit.
        /// If so, expand the limit.
        /// </summary>
        /// <param name="limit">The current limit.</param>
        /// <param name="value">The value to compare the limit to.</param>
        /// <param name="gt">True when comparison is greater-than. False if less-than.</param>
        public static float CalcBounds(float limit, double v, bool gt)
        {
#if DEBUG_REPORTS
            if (limit == 30730174)
            {
                File.AppendAllText(@"F:\temp\AE.txt",
                    "CalcBounds: limit " + limit + "\n");

            }
#endif
            float value = (float)v;
            if (gt)
            {
                if (value > limit)
                {
                    limit = value;
                }
            }
            else
            {
                if (value < limit)
                {
                    limit = value;
                }
            }
            return limit;
        }
    }
}
