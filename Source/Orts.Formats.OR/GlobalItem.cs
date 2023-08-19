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

using Newtonsoft.Json;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Drawing;

namespace Orts.Formats.OR
{
    /// <summary>
    /// GlobalItem: The generic item for the viewer and json
    /// </summary>
    public class GlobalItem
    {
        [JsonProperty("Location")]
        public PointF Location;
        [JsonProperty("Location2D")]
        public PointF Location2D;
        [JsonProperty("typeWidget")]
        public int typeItem;
        [JsonProperty("CoordMSTS")]
        public MSTSCoord Coord;
        [JsonProperty("NodeIDX")]
        public int associateNodeIdx { get; protected set; }
        [JsonProperty("SectionIDX")]
        public int associateSectionIdx { get; protected set; }
        [JsonProperty("inStationArea")]
        public bool inStationArea { get; set; }

        [JsonIgnore]
        private bool movable;
        [JsonIgnore]
        private bool rotable;
        [JsonIgnore]
        private bool editable;
        [JsonIgnore]
        private bool lineSnap;
        [JsonIgnore]
        private bool actEdit;
        [JsonIgnore]
        public bool isSeen;
        [JsonIgnore]
        public bool asMetadata { get; protected set; }  //  If true, the sideItem will be in the routeMetadata json file
        [JsonIgnore]
        public TrackNode associateNode { get; protected set; }  // Never save this information, it comes from MSTS

        /// <summary>
        /// The default constructor
        /// </summary>
        public GlobalItem()
        {
            movable = false;
            rotable = false;
            editable = false;
            lineSnap = false;
            actEdit = false;
            isSeen = false;
            asMetadata = false;
            typeItem = (int)TypeItem.GLOBAL_ITEM;
            Location = new PointF(float.NegativeInfinity, float.NegativeInfinity);
            Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
            Coord = new MSTSCoord();
        }

        public virtual void alignEdition(TypeEditor interfaceType, GlobalItem ownParent) { }

        public virtual void configCoord(MSTSCoord coord)
        {
            Coord = new MSTSCoord(coord);
            Location.X = coord.TileX * 2048f + coord.X;
            Location.Y = coord.TileY * 2048f + coord.Y;
        }

        public virtual void Update(MSTSCoord coord)
        {
        }

        public virtual void SynchroLocation()
        {
        }

        public virtual double FindItem(PointF point, double snap, double actualDist, MSTSItems aeItems)
        {
            double usedSnap = snap;
            isSeen = false;
            //snap =  1.0;// / snap;
            if ((((this.Location.X < (point.X - usedSnap)) || (Location.X > (point.X + usedSnap))) || (Location.Y < (point.Y - usedSnap))) || (this.Location.Y > (point.Y + usedSnap)))
            {
                return double.PositiveInfinity;
            }
            double dist = Math.Sqrt(Math.Pow((double)(Location.X - point.X), 2.0) + Math.Pow((double)(Location.Y - point.Y), 2.0));
            if (!(dist < usedSnap && actualDist == 0.0) && dist > actualDist)
            {
                return double.PositiveInfinity;
            }
            isSeen = true;
            return dist;
        }

        public virtual void complete(ORRouteConfig orRouteConfig, MSTSItems aeItems, MSTSBase tileBase) { }

        public virtual void Edit() { }

        public virtual void setAngle(float angle) { }

        public bool isItSeen() { return isSeen; }
        public bool IsMovable() { return movable; }
        public bool IsRotable() { return rotable; }
        public bool IsEditable() { return editable; }
        public bool IsLineSnap() { return lineSnap; }
        public bool IsActEditable() { return actEdit; }
        protected void setMovable() { movable = true; }
        protected void setRotable() { rotable = true; }
        protected void setEditable() { editable = true; }
        protected void setLineSnap() { lineSnap = true; }
        protected void setActEdit() { actEdit = true; }
        protected void unsetEditable() { editable = false; }
        public void Unreduce(MSTSBase tileBase)
        {
            Coord.Unreduce(tileBase);
        }
        public void Reduce(MSTSBase tileBase)
        {
            Coord.Reduce(tileBase);
            Location.X = Coord.TileX * 2048f + Coord.X;
            Location.Y = Coord.TileY * 2048f + Coord.Y;

        }
    }
}
