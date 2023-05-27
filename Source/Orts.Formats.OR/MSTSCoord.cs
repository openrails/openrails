// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.Drawing;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using ORTS.Common;

namespace Orts.Formats.OR
{
    public class MSTSBase
    {
        public double TileX { get; set; }
        public double TileY { get; set; }

        public MSTSBase()
        {
            TileX = 0;
            TileY = 0;
        }
        public MSTSBase(TrackDatabaseFile TDB)
        {
            double minTileX = double.PositiveInfinity;
            double minTileY = double.PositiveInfinity;

            TrackNode[] nodes = TDB.TrackDB.TrackNodes;
            for (int nodeIdx = 0; nodeIdx < nodes.Length; nodeIdx++)
            {
                if (nodes[nodeIdx] == null)
                    continue;
                TrackNode currNode = nodes[nodeIdx];
                if (currNode.TrVectorNode != null && currNode.TrVectorNode.TrVectorSections != null)
                {
                    if (currNode.TrVectorNode.TrVectorSections.Length > 1)
                    {
                        foreach (TrPin pin in currNode.TrPins)
                        {

                            if (minTileX > nodes[pin.Link].UiD.TileX)
                                minTileX = nodes[pin.Link].UiD.TileX;
                            if (minTileY > nodes[pin.Link].UiD.TileZ)
                                minTileY = nodes[pin.Link].UiD.TileZ;
                        }
                    }
                    else
                    {
                        TrVectorSection s;
                        s = currNode.TrVectorNode.TrVectorSections[0];
                        if (minTileX > s.TileX)
                            minTileX = s.TileX;
                        if (minTileY > s.TileZ)
                            minTileY = s.TileZ;
                    }
                }
                else if (currNode.TrJunctionNode != null)
                {
                    if (minTileX > currNode.UiD.TileX)
                        minTileX = currNode.UiD.TileX;
                    if (minTileY > currNode.UiD.TileZ)
                        minTileY = currNode.UiD.TileZ;
                }
            }
            TileX = minTileX;
            TileY = minTileY;
        }

        public MSTSCoord getMstsCoord(PointF point)
        {
            MSTSCoord coord = new MSTSCoord(point);
            return coord;
        }

        public void reduce(TrackDatabaseFile TDB)
        {
            TrackNode[] nodes = TDB.TrackDB.TrackNodes;
            for (int nodeIdx = 0; nodeIdx < nodes.Length; nodeIdx++)
            {
                if (nodes[nodeIdx] == null)
                    continue;
                ((TrackNode)TDB.TrackDB.TrackNodes[nodeIdx]).reduce(TileX, TileY);
            }
            if (TDB.TrackDB.TrItemTable == null)
                return;
            foreach (var item in TDB.TrackDB.TrItemTable)
            {
                item.TileX -= (int)TileX;
                item.TileZ -= (int)TileY;
            }
        }
    }

    public class MSTSCoord

    {
        public float TileX { get; set; }
        public float TileY { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        private bool Reduced = false;

        public MSTSCoord()
        {
        }

        public MSTSCoord(MSTSCoord coord)
        {
            TileX = coord.TileX;
            TileY = coord.TileY;
            X = coord.X;
            Y = coord.Y;
            Reduced = coord.Reduced;
        }

        public MSTSCoord(WorldLocation location)
        {
            TileX = location.TileX;
            TileY = location.TileZ;
            X = location.Location.X;
            Y = location.Location.Z;
            Reduced = true;
        }

        public MSTSCoord(TrVectorSection section)
        {
            TileX = section.TileX;
            TileY = section.TileZ;
            X = section.X;
            Y = section.Z;
            Reduced = section.Reduced;
        }

        public MSTSCoord(TrackNode node)
        {
            TileX = node.UiD.TileX;
            TileY = node.UiD.TileZ;
            X = node.UiD.X;
            Y = node.UiD.Z;
            Reduced = node.Reduced;
        }

        public MSTSCoord(PointF point)
        {
            point.X += 1024f;
            point.Y += 1024f;
            int signX = Math.Sign(point.X);
            int signY = Math.Sign(point.Y);
            int tileX = ((int)(point.X / 2048f));
            int tileY = ((int)(point.Y / 2048f));
            X = (float)((point.X) % 2048f);
            Y = (float)((point.Y) % 2048f);
            if (signX < 0)
            {
                tileX -= 1;
                X += 2048f;
            }
            if (signY < 0)
            {
                tileY -= 1;
                Y += 2048f;
            }
            TileX = tileX;
            TileY = tileY;
            X -= 1024f;
            Y -= 1024F;
            Reduced = true;
        }

        public void Unreduce(MSTSBase tileBase)
        {
            if (Reduced)
            {
                TileX += (int)tileBase.TileX;
                TileY += (int)tileBase.TileY;
            }
            Reduced = false;
        }

        public void Reduce(MSTSBase tileBase)
        {
            if (!Reduced)
            {
                TileX -= (int)tileBase.TileX;
                TileY -= (int)tileBase.TileY;
            }
            Reduced = true;
        }

        // Equality operator. test if the coordinates are at the same point.
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            MSTSCoord other = (MSTSCoord)obj;
            return (this.X == other.X && this.Y == other.Y && this.TileX == other.TileX && this.TileY == other.TileY);
        }

        public static bool operator ==(MSTSCoord x, MSTSCoord y)
        {
            return Object.Equals(x, y);
        }

        public static bool operator !=(MSTSCoord x, MSTSCoord y)
        {
            return !Object.Equals(x, y);
        }

        public static bool near(MSTSCoord x, MSTSCoord y)
        {
            float squareA = (float)Math.Pow((x.X - y.X), 2);
            float squareB = (float)Math.Pow((x.Y - y.Y), 2);
            float AX = (float)Math.Round((Decimal)(x.X - y.Y), 2, MidpointRounding.ToEven);
            float AY = (float)Math.Round((Decimal)x.Y, 2, MidpointRounding.ToEven);
            float BX = (float)Math.Round((Decimal)y.X, 2, MidpointRounding.ToEven);
            float BY = (float)Math.Round((Decimal)y.Y, 2, MidpointRounding.ToEven);

            if ((float)Math.Round((Decimal)(squareA + squareB)) < 0.1f && x.TileX == y.TileX && x.TileY == y.TileY)
                return true;
            return false;
        }

        public override int GetHashCode()
        {   // based on http://stackoverflow.com/questions/5221396/what-is-an-appropriate-gethashcode-algorithm-for-a-2d-point-struct-avoiding
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + TileX.GetHashCode();
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + TileY.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }


        public PointF ConvertToPointF()
        {
            PointF val = new PointF(0, 0);
            val.X = ((TileX * 2048f) + X);  // - minX) ;
            val.Y = ((TileY * 2048f) + Y);  // - minY) ;
            return val;
        }

        public dVector ConvertVector()
        {
            return new dVector((TileX * 2048f) + X, (TileY * 2048f) + Y);
        }

        public Vector2 ConvertVector2()
        {
            return new Vector2((float)((TileX * 2048f) + X), (float)((TileY * 2048f) + Y));
        }


        public string asString()
        {
            //File.AppendAllText(@"F:\temp\AE.txt", "Coord: X:" + TileX + ".." + X + "/ Y:" + TileY + ".." + Y);
            string X1 = string.Format("{0:d}", (int)((TileX * 2048f) + X));
            string Y1 = string.Format("{0:d}", (int)((TileY * 2048f) + Y));
            string info = "(" + X1 + "," + Y1 + ")";
            //File.AppendAllText(@"F:\temp\AE.txt", "/ donne :" + info + "\n");
            return info;
        }
    }

}
