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

using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Orts.Formats.OR
{
    /// <summary>
    /// MSTSItems retains all the items comming from MSTS route config represented by GlobalItem derived classes.
    /// </summary>
    public class MSTSItems
    {
        public List<AEJunctionItem> switches;
        public List<AESignalItem> signals;
        public List<SideItem> sidings;
        public List<AEBufferItem> buffers;
        public List<TrackSegment> segments;
        public List<ShapeItem> shapes;
        public List<AECrossOver> aeCrossOver;

        /// <summary>
        /// The MSTSItems class constructor.
        /// </summary>
        public MSTSItems()
        {
            signals = new List<AESignalItem>();
            sidings = new List<SideItem>();
            switches = new List<AEJunctionItem>();
            buffers = new List<AEBufferItem>();
            segments = new List<TrackSegment>();
            shapes = new List<ShapeItem>();
            aeCrossOver = new List<AECrossOver>();
        }

        /// <summary>
        /// Used to add a new segment to the list of segments managed by the editor.
        /// </summary>
        /// <param name="line">A TrackSegment instance to be added.</param>
        public void AddSegment(TrackSegment line)
        {
            PointF lineA = line.getStart();
            PointF lineB = line.getEnd();
            if (lineA == lineB)
                return;
            foreach (var segment in segments)
            {
                PointF segmentA = segment.associateSegment.startPoint;
                PointF segmentB = segment.associateSegment.endPoint;
                if (lineA == segmentA && lineB == segmentB)
                    return;
                if (lineA == segmentB && lineB == segmentA)
                    return;
            }
            segments.Add(line);
        }

        /// <summary>
        /// Used to add a new siding (start or end) to the list of siding  managed by the editor.
        /// </summary>
        /// <param name="trSegment">The TrackSegment associated to the siding as TrackSegment instance.</param>
        /// <param name="info"></param>
        /// <param name="travel"></param>
        /// <returns>Return a new SideItem instance</returns>
        public SideItem AddSiding(TrackSegment trSegment, TrItem info, AETraveller travel)
        {
            SideItem sideItem = (SideItem)sidings.Find(place => place.Name == info.ItemName && place.trItem.TrItemId == info.TrItemId);
            if (sideItem == null)
            {
                SideItem relatedItem = (SideItem)sidings.Find(place => place.Name == info.ItemName && place.trItem.TrItemId != info.TrItemId);
                if (relatedItem == null)
                {
                    sideItem = new SideStartItem(trSegment, info);
                    sidings.Add(sideItem);
                    trSegment.AddSiding((SideItem)sideItem);
                }
                else
                {
                    travel.place(trSegment.associateNode);
                    travel.MoveTo(info);
                    float dist = travel.DistanceTo(relatedItem.trItem);
                    if (dist < 0)
                    {
                        travel.ReverseDirection();
                        dist = travel.DistanceTo(relatedItem.trItem);
                    }
                    ((SideStartItem)relatedItem).sizeSiding = dist;
                    sideItem = new SideEndItem(trSegment, info, (SideStartItem)relatedItem);
                    ((SideEndItem)sideItem).sizeSiding = dist;
                    ((SideStartItem)relatedItem).endSiding = (SideEndItem)sideItem;
                    sidings.Add(sideItem);
                    trSegment.AddSiding((SideItem)sideItem);
                }
            }
            return sideItem;
        }

        public AECrossOver AddCrossOver(TrackSegment trSegment, TrItem info, AETraveller travel)
        {
            AECrossOver crossOver = (AECrossOver)aeCrossOver.Find(place => place.SData1 == info.SData1);
            if (crossOver == null)
            {
                crossOver = new AECrossOver(trSegment, info);
                aeCrossOver.Add(crossOver);
                trSegment.AddCrossOver((AECrossOver)crossOver);
                return crossOver;
            }
            crossOver.setCrossSegment(trSegment);
            return crossOver;
        }

        public void AddSignal(AESignalItem signal)
        {
            signals.Add(signal);
        }

        /// <summary>
        /// Search through 'segments', 'switches' or 'buffer' for the item in relation with the TrackNode index
        /// </summary>
        /// <param name="nodeIdx">The TrackNode index to search for</param>
        /// <returns>GlobalItem, use the typeItem as 'TypeItem' to do the casting</returns>
        public GlobalItem GetTrackSegment(uint nodeIdx)
        {
            foreach (var item in segments)
            {
                if (item.associateNodeIdx == nodeIdx)
                    return (GlobalItem)item;
            }
            foreach (var item in switches)
            {
                if (item.associateNodeIdx == nodeIdx)
                    return (GlobalItem)item;
            }
            foreach (var item in buffers)
            {
                if (item.associateNodeIdx == nodeIdx)
                    return (GlobalItem)item;
            }
            return null;
        }

        /// <summary>
        /// Search through 'segments', 'switches' or 'buffer' for the item in relation with the given TrackNode and sectionIdx
        /// </summary>
        /// <param name="node">The TrackNode to search for</param>
        /// <param name="sectionIdx">in case of multiple VectorNode, the index of the relevant vector</param>
        /// <returns>GlobalItem, use the typeItem as 'TypeItem' to do the casting</returns>
        public GlobalItem GetTrackSegment(TrackNode node, int sectionIdx)
        {
            if (node.TrJunctionNode != null)
            {
                foreach (var item in switches)
                {
                    if (item.associateNode.TrJunctionNode.Idx == node.Index)
                        return (GlobalItem)item;
                }
            }
            else if (node.TrEndNode)
            {
                foreach (var item in buffers)
                {
                    if (item.associateNode.Index == node.Index)
                        return (GlobalItem)item;
                }
            }
            else if (sectionIdx >= 0)
            {
                //foreach (var sideItem in segments)
                for (int cnt = 0; cnt < segments.Count; cnt++)
                {
                    var item = segments[cnt];
                    if (item.associateNodeIdx == node.Index && item.associateSectionIdx == sectionIdx)
                        return (GlobalItem)item;
                }
                foreach (var item in segments)
                {
                    if (item.associateNodeIdx == node.Index)
                        return (GlobalItem)item;
                }
            }
            return null;
        }

        /// <summary>
        /// Search through 'segments' for the item in relation with the given TrackNodeIdx and sectionIdx
        /// This methof return only a TrackSegment
        /// </summary>
        /// <param name="nodeIdx">The index of the node in TrackNode</param>
        /// <param name="sectionIdx">The index of the vector in the TrackNode</param>
        /// <returns>TrackSegment</returns>
        public TrackSegment GetTrackSegment(int nodeIdx, int sectionIdx)
        {
            TrackSegment trackSegment = null;
            foreach (var segment in segments)
            {
                if (segment.associateNodeIdx == nodeIdx && segment.associateSectionIdx == sectionIdx)
                {
                    trackSegment = segment;
                    break;
                }
            }

            return trackSegment;
        }

        /// <summary>
        /// Search a segment from the coordinate of the mouse.
        /// </summary>
        /// <param name="pt">The real coordinate pointed by the mouse</param>
        /// <param name="snapSize">A circle in wich the segment must cross</param>
        /// <returns>GlobalItem but for now, only a TrackSegment</returns>
        public GlobalItem findSegmentFromMouse(PointF pt, double snapSize)
        {
            double positiveInfinity = double.PositiveInfinity;
            
            if (snapSize < 1.0)
            {
                snapSize = 1.0;
            }
            
            PointF closest = new PointF(0f, 0f);
            TrackSegment segment = null;
            foreach (TrackSegment segment2 in segments)
            {
                segment2.unsetSnap();
                double num = DrawUtility.FindDistanceToSegment(pt, segment2, out closest);
                if ((num < snapSize) && (num < positiveInfinity))
                {
                    positiveInfinity = num;
                    segment = segment2;
                }
            }
            if (segment != null)
            {
                segment.setSnaps(this);
                return segment;
            }
            return null;
        }

        /// <summary>
        /// Return the list of buffer
        /// </summary>
        /// <returns>List<AEBufferItem></returns>
        public List<AEBufferItem> getBuffers()
        {
            return buffers;
        }

        /// <summary>
        /// Return the list of segments
        /// </summary>
        /// <returns>List<TrackSegment></returns>
        public List<TrackSegment> getSegments()
        {
            return segments;
        }

        /// <summary>
        /// Search for a shape by index, if no occurence, create one and return it
        /// Work in Progress
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public ShapeItem GetShape(int idx)
        {
            ShapeItem item = shapes.Find(place => place.ShapeIdx == idx);
            if (item == null)
            {
                item = new ShapeItem(idx);
                shapes.Add(item);
            }
            return item;
        }

        public List<ShapeItem> getShapes()
        {
            return shapes;
        }

        public List<SideItem> getSidings()
        {
            return sidings;
        }

        public List<AECrossOver> getCrossOver()
        {
            return aeCrossOver;
        }

        public List<AESignalItem> getSignals()
        {
            return signals;
        }

        public List<AEJunctionItem> getSwitches()
        {
            return switches;
        }
    }

    #region TagItem

    /// <summary>
    /// Used to represent a tag, a mark with a name and used to facilitate navigation in the viewer 
    /// </summary>
    public class TagItem : GlobalItem
    {
        [JsonProperty("nameTag")]
        public string nameTag { get; set; }
        [JsonProperty("nameVisible")]
        public bool nameVisible;

        public TagItem(TypeEditor interfaceType)
        {
            alignEdition(interfaceType, null);
        }

        public override void alignEdition(TypeEditor interfaceType, GlobalItem ownParent)
        { 
            setMovable();
            asMetadata = true;
        }

        public void setNameTag(int info)
        {
            nameTag = "tag" + info;
        }

        public override void configCoord (MSTSCoord coord)
        {
            base.configCoord(coord);
            typeItem = (int)TypeItem.TAG_ITEM;
            nameVisible = false;
        }

        public override void Update(MSTSCoord coord)
        {
            base.configCoord(coord);
        }
    }
    #endregion

    #region StationItem

    public class StationItem : GlobalItem
    {
        [JsonProperty("nameStation")]
        public string nameStation;
        [JsonProperty("nameVisible")]
        public bool nameVisible;
        [JsonProperty("stationArea")]
        public List<StationAreaItem> stationArea;
        [JsonProperty("icoAngle")]
        public float icoAngle;
        [JsonProperty("areaCompleted")]
        public bool areaCompleted;
        [JsonProperty("configuredBuffer")]
        public List<AEBufferItem> insideBuffers;

        [JsonIgnore]
        private List<TrackSegment> segmentsInStation;
        [JsonIgnore]
        public AETraveller traveller { get; protected set; }
        [JsonIgnore]
        public StationPathsHelper StationPathsHelper;

        public StationItem(TypeEditor interfaceType, AETraveller travel)
        {
            icoAngle = 0f;
            stationArea = new List<StationAreaItem>();
            alignEdition(interfaceType, null);
            areaCompleted = false;
            this.segmentsInStation = new List<TrackSegment>();
            this.traveller = travel;
            insideBuffers = new List<AEBufferItem>();
            StationPathsHelper = new StationPathsHelper(this.GetPaths);
        }

        public override void alignEdition(TypeEditor interfaceType, GlobalItem ownParent)
        {
            if (interfaceType == TypeEditor.ROUTECONFIG)
            {
                setMovable();
                setRotable();
                setEditable();
                asMetadata = true;
            }
        }

        public void setNameStation(int info)
        {
            nameStation = "station" + info;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
            typeItem = (int)TypeItem.STATION_ITEM;
            nameVisible = false;
        }

        public List<StationAreaItem> getStationArea()
        {
            return stationArea;
        }

        public StationAreaItem getNextArea(StationAreaItem cur)
        {
            if ((cur == null) && (this.stationArea.Count<StationAreaItem>() <= 0))
            {
                return null;
            }
            int num = 0;
            while (num < this.stationArea.Count<StationAreaItem>())
            {
                if (this.stationArea[num] == cur)
                {
                    break;
                }
                num++;
            }
            if (num > this.stationArea.Count<StationAreaItem>())
            {
                return this.stationArea[0];
            }
            return this.stationArea[num];

        }

        public override void Update(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public override void setAngle(float angle)
        {
            icoAngle = angle;
        }

#if false
        public StationAreaItem AddPointArea(MSTSCoord coord, int snapDist, MSTSBase tileBase)
        {
            PointF found = new PointF(0, 0);
            double dist = -1, closestDist = double.PositiveInfinity;
            int i;
            int closestI = -1;
            PointF closest;

            StationAreaItem newPoint = new StationAreaItem(TypeEditor.ROUTECONFIG, this);

            if (areaCompleted)
            {   // More than 4 points, we try to insert
                StationAreaItem p1;
                StationAreaItem p2;
                for (i = 0; i < stationArea.Count - 1; i++)
                {
                    p1 = stationArea[i];
                    p2 = stationArea[i + 1];
                    dist = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(),
                        new AESegment(p1.Coord.ConvertToPointF(), p2.Coord.ConvertToPointF()), out found);
                    if (dist >= 0 && dist < snapDist && dist < closestDist)
                    {
                        closestDist = dist;
                        closestI = i + 1;
                        closest = found;
                    }
                    dist = -1;
                }
                p1 = stationArea[stationArea.Count - 1];
                p2 = stationArea[0];
                dist = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(),
                    new AESegment(p1.Coord.ConvertToPointF(), p2.Coord.ConvertToPointF()), out closest);

                if (dist >= 0 && dist < snapDist && dist < closestDist)
                {
                    closestDist = dist;
                    closestI = 0;
                    closest = found;
                }
                if (closestDist < double.PositiveInfinity)
                {
                    coord = tileBase.getMstsCoord(closest);
                    newPoint.configCoord(coord);
                    newPoint.toggleSelected();
                    stationArea.Insert(closestI, newPoint);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                //  Until area complete, we only add new point at the end
                newPoint.configCoord(coord);
                newPoint.toggleSelected();
                stationArea.Add(newPoint);
            }
            return newPoint;
        }
        
#endif
        public StationAreaItem AddPointArea(MSTSCoord coord, double snapDist, MSTSBase tileBase)
        {
            PointF closest = new PointF(0f, 0f);
            double num = -1.0;
            double positiveInfinity = double.PositiveInfinity;
            int index = -1;
            StationAreaItem item = new StationAreaItem(TypeEditor.ROUTECONFIG, this);
            if (this.areaCompleted)
            {
                PointF tf2;
                StationAreaItem item2;
                StationAreaItem item3;
                for (int i = 0; i < (this.stationArea.Count - 1); i++)
                {
                    item2 = this.stationArea[i];
                    item3 = this.stationArea[i + 1];
                    num = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(), new AESegment(item2.Coord.ConvertToPointF(), item3.Coord.ConvertToPointF()), out closest);
                    if (((num >= 0.0) && (num < snapDist)) && (num < positiveInfinity))
                    {
                        positiveInfinity = num;
                        index = i + 1;
                        tf2 = closest;
                    }
                    num = -1.0;
                }
                item2 = this.stationArea[this.stationArea.Count - 1];
                item3 = this.stationArea[0];
                num = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(), new AESegment(item2.Coord.ConvertToPointF(), item3.Coord.ConvertToPointF()), out tf2);
                if (((num >= 0.0) && (num < snapDist)) && (num < positiveInfinity))
                {
                    positiveInfinity = num;
                    index = 0;
                    tf2 = closest;
                }
                if (positiveInfinity >= double.PositiveInfinity)
                {
                    return null;
                }
                coord = tileBase.getMstsCoord(tf2);
                item.configCoord(coord);
                item.toggleSelected();
                stationArea.Insert(index, item);
                return item;
            }
            item.configCoord(coord);
            item.toggleSelected();
            stationArea.Add(item);
            return item;
        }

        public void AddBuffers(ORRouteConfig orRouteConfig, List<AEBufferItem> buffers)
        {
            List<System.Drawing.PointF> polyPoints = getPolyPoints();
            if (insideBuffers.Count > 0)
            {
                insideBuffers.Clear();
            }
            foreach (AEBufferItem buffer in buffers)
            {
                if (DrawUtility.PointInPolygon(buffer.Location, polyPoints)) // && buffer.Configured && buffer.DirBuffer != AllowedDir.NONE)
                {
                    insideBuffers.Add(buffer);
                    buffer.alignEdition(TypeEditor.ROUTECONFIG, this);
                    orRouteConfig.AddItem(buffer);
                }
            }
        }

        public override void complete(ORRouteConfig orRouteConfig, MSTSItems aeItems, MSTSBase tileBase)
        {
            if (stationArea.Count > 0)
            {
                AddBuffers(orRouteConfig, aeItems.buffers);
                checkForNewConnector(orRouteConfig, aeItems, tileBase);
                areaCompleted = true;
                searchForPaths(orRouteConfig, aeItems, tileBase);
                GetPaths();
            }
        }
        public void GetPaths()
        {
            List<StationPath> paths = null;
            for (int i = 0; i < this.stationArea.Count; i++)
            {
                StationAreaItem item = this.stationArea[i];
                if (item.IsInterface() && item.stationConnector.getLineSegment() != null &&
                    (item.stationConnector.getDirConnector() == AllowedDir.IN ||
                    item.stationConnector.getDirConnector() == AllowedDir.InOut))
                {
                    paths = item.stationConnector.stationPaths.getPaths();
                    StationPathsHelper.Add(item.stationConnector.getLabel(), paths);
                }
            }
            foreach (AEBufferItem buffer in insideBuffers)
            {
                if (buffer.stationPaths == null)
                    continue;
                paths = buffer.stationPaths.getPaths();
                StationPathsHelper.Add(buffer.NameBuffer, paths);
            }
        }

        public List<System.Drawing.PointF> getPolyPoints() //  Or Polygone, it's the same
        {
            List<System.Drawing.PointF> polyPoints = new List<System.Drawing.PointF>();
            foreach (StationAreaItem SAItem in stationArea)
            {
                float X = (SAItem.Coord.TileX * 2048f + SAItem.Coord.X);
                float Y = SAItem.Coord.TileY * 2048f + SAItem.Coord.Y;
                //  TODO: Le spolypoint doivent avoir des coordonnées absolues sur la map
                polyPoints.Add(new System.Drawing.PointF(X, Y));
            }
            return polyPoints;
        }

#if false
        public List<System.Drawing.PointF> getPolyEdge() //  List of edges for this area
        {
            float X, Y;

            if (stationArea.Count <= 0)
                return null;
            List<System.Drawing.PointF> polyPoints = new List<System.Drawing.PointF>();
            foreach (StationAreaItem SAItem in stationArea)
            {
                X = (SAItem.Coord.TileX * 2048f + SAItem.Coord.X);
                Y = SAItem.Coord.TileY * 2048f + SAItem.Coord.Y;
                polyPoints.Add(new System.Drawing.PointF(X, Y));
            }
            X = (stationArea[0].Coord.TileX * 2048f + stationArea[0].Coord.X);
            Y = stationArea[0].Coord.TileY * 2048f + stationArea[0].Coord.Y;
            polyPoints.Add(new System.Drawing.PointF(X, Y));

            return polyPoints;
        }
        
#endif
        public List<AESegment> getPolySegment()
        {
            StationAreaItem item;
            StationAreaItem item2;
            PointF tf;
            PointF tf2;
            if (this.stationArea.Count <= 0)
            {
                return null;
            }
            List<AESegment> list = new List<AESegment>();
            for (int i = 0; i < (this.stationArea.Count - 1); i++)
            {
                item = this.stationArea[i];
                item2 = this.stationArea[i + 1];
                tf = new PointF((item.Coord.TileX * 2048f) + item.Coord.X, (item.Coord.TileY * 2048f) + item.Coord.Y);
                tf2 = new PointF((item2.Coord.TileX * 2048f) + item2.Coord.X, (item2.Coord.TileY * 2048f) + item2.Coord.Y);
                list.Add(new AESegment(tf, tf2));
            }
            item = this.stationArea[0];
            item2 = this.stationArea[this.stationArea.Count - 1];
            tf = new PointF((item.Coord.TileX * 2048f) + item.Coord.X, (item.Coord.TileY * 2048f) + item.Coord.Y);
            tf2 = new PointF((item2.Coord.TileX * 2048f) + item2.Coord.X, (item2.Coord.TileY * 2048f) + item2.Coord.Y);
            list.Add(new AESegment(tf2, tf));
            return list;
        }

        public override double FindItem(PointF point, double snap, double actualDist, MSTSItems aeItems)
        {
            double iconDist = double.PositiveInfinity;
            List<System.Drawing.PointF> poly = getPolyPoints();
            int i, j = poly.Count - 1;
            bool oddNodes = false;
            double dist = double.PositiveInfinity;
            double usedSnap = snap;

            isSeen = false;
            if (!((Location.X < point.X - usedSnap) || (Location.X > point.X + usedSnap)
                || (Location.Y < point.Y - usedSnap) || (Location.Y > point.Y + usedSnap)))
            {
                isSeen = true;
                iconDist = (Math.Sqrt(Math.Pow((Location.X - point.X), 2) + Math.Pow((Location.Y - point.Y), 2)));
            }
            //File.AppendAllText(@"F:\temp\AE.txt", "FindItem: pointX: " + point.X +
            //    " pointY: " + point.Y + "\n");
            for (i = 0; i < poly.Count; i++)
            {
                dist = ((StationAreaItem)stationArea[i]).FindItem(point, usedSnap, iconDist < actualDist ? iconDist : actualDist, aeItems);
                if (stationArea[i].isSeen)
                {
                    isSeen = false;
                    return dist;
                }
                //File.AppendAllText(@"F:\temp\AE.txt", "FindItem: polyX" + poly[i].X + 
                //    " polyY: " + poly[i].Y + 
                //    " polyX(j): " + poly[j].X +
                //    " polyY (j): " + poly[j].Y + "\n");
                if ((poly[i].Y < point.Y && poly[j].Y >= point.Y ||
                    poly[j].Y < point.Y && poly[i].Y >= point.Y)
                && (poly[i].X <= point.X || poly[j].X <= point.X))
                {
                    oddNodes ^= (poly[i].X + (point.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < point.X);
                    //File.AppendAllText(@"F:\temp\AE.txt", "oddNodes ^=\n");
                }
                j = i;
            }

            if (oddNodes)
            {
                highlightTrackFromArea(aeItems);
                return 0;
            }
            return iconDist;
        }

        public double FindItemExact(PointF point, double actualDist, MSTSItems aeItems)
        {
            return FindItem(point, 0.0, actualDist, aeItems);
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            foreach (StationAreaItem item in stationArea)
            {
                item.highlightTrackFromArea(aeItems);
            }
            foreach (AEBufferItem buffer in insideBuffers)
            {
                buffer.highlightTrackFromArea(aeItems);
            }
        }

        public void searchForPaths(ORRouteConfig orRouteConfig, MSTSItems aeItems, MSTSBase tileBase)
        {
            List<StationPath> paths = null;
            //StationPathsHelper.Clear();
            
            for (int i = 0; i < this.stationArea.Count; i++)
            {
                double positiveInfinity = double.PositiveInfinity;
                StationAreaItem item = this.stationArea[i];
                if (item.IsInterface() && item.stationConnector.getLineSegment() != null &&
                    (item.stationConnector.getDirConnector() == AllowedDir.IN ||
                    item.stationConnector.getDirConnector() == AllowedDir.InOut))
                {
                    TrackSegment segment = item.stationConnector.getLineSegment();
                    AETraveller myTravel = new AETraveller(this.traveller);
                    myTravel.place(segment.associateNodeIdx, (int)item.Coord.TileX, (int)item.Coord.TileY, item.Coord.X, item.Coord.Y);
                    myTravel.Move(1.0f);
                    PointF position = myTravel.getCoordinate();
                    if (FindItemExact(position, positiveInfinity, aeItems) == 0.0)
                    {
                        paths = item.stationConnector.searchPaths(myTravel, getListConnector(), aeItems, this);
                    }
                    else
                    {
                        myTravel.ReverseDirection();
                        myTravel.Move(2.0f);
                        position = myTravel.getCoordinate();
                        if (this.FindItemExact(position, positiveInfinity, aeItems) == 0.0)
                        {
                            paths = item.stationConnector.searchPaths(myTravel, getListConnector(), aeItems, this);
                        }
                    }
                    //StationPathsHelper.Add(item.stationConnector.getLabel(), paths);
                }
            }
            foreach (AEBufferItem buffer in insideBuffers)
            {
                AETraveller myTravel = new AETraveller(this.traveller);
                myTravel.place((int)buffer.Coord.TileX, (int)buffer.Coord.TileY, buffer.Coord.X, buffer.Coord.Y);
                if (myTravel.EndNodeAhead() != null)
                    myTravel.ReverseDirection();
                paths = buffer.searchPaths(myTravel, getListConnector(), aeItems, this);
                //StationPathsHelper.Add(buffer.NameBuffer, paths);
            }
        }

        public void checkForNewConnector(ORRouteConfig orRouteConfig, MSTSItems mstsItems, MSTSBase tileBase)
        {
            //  First, we remove all un configured connectors
            //foreach (StationAreaItem SAWidget in stationArea)
            for (int cnt = 0; cnt < stationArea.Count; cnt++)
            {
                var SAItem = stationArea[cnt];
                if (SAItem.IsInterface() && !SAItem.getStationConnector().isConfigured())
                {
                    removeConnector(orRouteConfig, SAItem);
                    cnt--;
                }
            }
            //  Next, we search for new connectors and add them
            foreach (var item in stationArea)
            {
                if (!item.IsInterface())
                    continue;
                TrackSegment trackSegment = mstsItems.GetTrackSegment(item.associateNodeIdx, item.associateSectionIdx);
                item.DefineAsInterface(trackSegment);
            }
            foreach (var segment in mstsItems.segments)
            {
                int num = 0;
                List<System.Drawing.PointF> pointsIntersect = new List<System.Drawing.PointF>();
                
                List<AESegment> polySegments = getPolySegment();
                if (segment.associateNodeIdx == 344)
                    num = 0;
                for (int cntPointSegment = 0; cntPointSegment < polySegments.Count; cntPointSegment++)
                {
                    PointF pointIntersect = DrawUtility.FindIntersection(polySegments[cntPointSegment], new AESegment (segment));
                    if (!pointIntersect.IsEmpty)
                    {
                        StationAreaItem newPoint = new StationAreaItem(TypeEditor.ROUTECONFIG, this);
                        MSTSCoord coord = tileBase.getMstsCoord(pointIntersect);
                        newPoint.configCoord(coord);
                        num++;
                        //newPoint.toggleSelected();
                        stationArea.Insert(num, newPoint);
                        newPoint.DefineAsInterface(segment);
                        newPoint.setAngle(getPolyPoints());
                        orRouteConfig.AddItem(newPoint);
                    }
                    num++;
                }
            }
        }

        public void removeConnector(ORRouteConfig orRouteConfig, StationAreaItem connector)
        {
            stationArea.Remove(connector);
            orRouteConfig.RemoveConnectorItem(connector);

        }

        public void setTraveller(AETraveller travel)
        {
            this.traveller = travel;
        }

        private List<TrackSegment> getListConnector()
        {
            List<TrackSegment> list = new List<TrackSegment>();
            for (int i = 0; i < this.stationArea.Count; i++)
            {
                StationAreaItem item = this.stationArea[i];
                if (item.IsInterface() && item.stationConnector != null && item.stationConnector.getLineSegment() != null)
                {
                    list.Add(item.stationConnector.getLineSegment());
                }
            }
            return list;
        }

        public bool IsInStation(MSTSCoord place)
        {
            double iconDist = double.PositiveInfinity;
            List<System.Drawing.PointF> poly = getPolyPoints();
            int i, j = poly.Count - 1;
            bool oddNodes = false;
            PointF placeNormalized = place.ConvertToPointF();

            isSeen = false;
            if (!((Location.X < placeNormalized.X) || (Location.X > placeNormalized.X)
                || (Location.Y < placeNormalized.Y) || (Location.Y > placeNormalized.Y)))
            {
                isSeen = true;
                iconDist = (Math.Sqrt(Math.Pow((Location.X - placeNormalized.X), 2) + Math.Pow((Location.Y - placeNormalized.Y), 2)));
            }
            //File.AppendAllText(@"F:\temp\AE.txt", "FindItem: pointX: " + point.X +
            //    " pointY: " + point.Y + "\n");
            for (i = 0; i < poly.Count; i++)
            {
                if ((poly[i].Y < placeNormalized.Y && poly[j].Y >= placeNormalized.Y ||
                    poly[j].Y < placeNormalized.Y && poly[i].Y >= placeNormalized.Y)
                && (poly[i].X <= placeNormalized.X || poly[j].X <= placeNormalized.X))
                {
                    oddNodes ^= (poly[i].X + (placeNormalized.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < placeNormalized.X);
                    //File.AppendAllText(@"F:\temp\AE.txt", "oddNodes ^=\n");
                }
                j = i;
            }

            if (oddNodes)
            {
                return true;
            }
            return false;
        }
    }
    #endregion

    #region StationAreaItem


    public class StationAreaItem : GlobalItem
    {
        [JsonProperty("Connector")]
        public StationConnector stationConnector = null;
        [JsonIgnore]
        bool selected = false;
        [JsonIgnore]
        public StationItem parent { get; protected set; }

        public StationAreaItem(TypeEditor interfaceType, StationItem myParent)
        {
            alignEdition(interfaceType, myParent);
            parent = myParent;
        }

        public override void alignEdition(TypeEditor interfaceType, GlobalItem ownParent)
        {
            if (interfaceType == TypeEditor.ROUTECONFIG)
            {
                setMovable();
                asMetadata = true;
            }
            if (parent == null)
                parent = (StationItem)ownParent;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public void DefineAsInterface(TrackSegment segment)
        {
            typeItem = (int)TypeItem.STATION_CONNECTOR;
            if (stationConnector == null)
            {
                stationConnector = new StationConnector(segment);
            }
            stationConnector.Init(segment);
            associateNode = segment.associateNode;
            associateNodeIdx = segment.associateNodeIdx;
            associateSectionIdx = segment.associateSectionIdx;
            setLineSnap();
            setEditable();
            asMetadata = true;
        }

        public void setAngle(List<System.Drawing.PointF> polyPoint)
        {
            stationConnector.setIcoAngle(Coord.ConvertToPointF(), polyPoint);
        }

        public bool toggleSelected ()
        {
            selected = !selected;
            return selected;
        }

        public override void Update(MSTSCoord coord)
        {   
                base.configCoord(coord);
        }

        public bool IsInterface()
        {
            if (typeItem == (int)TypeItem.STATION_CONNECTOR)
                return true;
            else
                return false;
        }

        public override void Edit()
        {
        }

        public StationConnector getStationConnector()
        {
            return stationConnector;
        }

        public void setInterfaceConfigured()
        {
            if (IsInterface())
                stationConnector.setConfigured();
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            if (IsInterface())
            {
                stationConnector.highlightTrackFromArea(aeItems);
            }
        }

        public void BeRemove()
        {
            if (IsInterface())
            {
            }
        }
    }

    public class StationConnector
    {
        [JsonProperty("dirConnector")]
        public AllowedDir dirConnector { get; protected set; }
        [JsonProperty("labelConnector")]
        public string label { get; protected set; }
        [JsonIgnore]
        List<string> allowedDirections;
        [JsonIgnore]
        public TrackSegment segment { get; protected set; }
        [JsonPropertyAttribute("angle")]
        public float angle;
        [JsonProperty("Configured")]
        bool configured;
        [JsonProperty("StationPaths")]
        public StationPaths stationPaths { get; protected set; }
        [JsonProperty("ChainedConnector")]
        public string ChainedConnector { get; protected set; }    // Circle chain
        [JsonConstructor]
        public StationConnector(int i)
        {
            //File.AppendAllText(@"F:\temp\AE.txt", "Json StationConnector :" + (int)i + "\n");
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            dirConnector = AllowedDir.NONE;
            configured = false;
            stationPaths = new StationPaths();
            label = "";
        }

        public StationConnector(TrackSegment segment)
        {
            //File.AppendAllText(@"F:\temp\AE.txt", "StationConnector\n");
            this.segment = segment;
            segment.HasConnector = this;
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            dirConnector = AllowedDir.NONE;
            configured = false;
            stationPaths = new StationPaths();
            label = "";
        }

        public void Init(TrackSegment info)
        {
            segment = info;
            info.HasConnector = this;
        }

        public void setConfigured()
        {
            configured = true;
        }

        public bool isConfigured() { return configured; }
        public string getLabel()
        {
            return label;
        }

        public void setLabel(string info)
        {
            label = info;
        }

        public void setDirConnector(string info)
        {
            if (info == allowedDirections[1])
                dirConnector = AllowedDir.IN;
            else if (info == allowedDirections[2])
                dirConnector = AllowedDir.OUT;
            else if (info == allowedDirections[3])
                dirConnector = AllowedDir.InOut;
            else
                dirConnector = AllowedDir.NONE;
        }

        public AllowedDir getDirConnector()
        {
            return dirConnector;
        }

        public List<string> getAllowedDirections()
        {
            return allowedDirections;
        }

        public TrackSegment getLineSegment()
        {
            return segment;
        }

        public void setIcoAngle(PointF posit, List<PointF> polyPoints)
        {
            double tempo;
            PointF end1 = segment.associateSegment.startPoint;
            PointF end2 = segment.associateSegment.endPoint;

            if (DrawUtility.PointInPolygon(end1, polyPoints))
            {
                tempo = Math.Atan2(end1.X - posit.X, end1.Y - posit.Y);
                angle = (float)((tempo * 180.0d) / Math.PI) - 90f;
            }
            else if (DrawUtility.PointInPolygon(end2, polyPoints))
            {
                tempo = Math.Atan2(end2.X - posit.X, end2.Y - posit.Y);
                angle = (float)((tempo * 180.0d) / Math.PI) - 90f;
            }
            else
            {
                angle = 0.0f;
            }
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            if (stationPaths != null)
            {
                stationPaths.highlightTrackFromArea(aeItems);
            }
        }

        public List<StationPath> searchPaths(AETraveller myTravel, List<TrackSegment> listConnector, MSTSItems aeItems, StationItem parent)
        {
            if (stationPaths == null)
            {
                stationPaths = new StationPaths();
            }
            stationPaths.Clear();
            List<StationPath> paths = stationPaths.explore(myTravel, listConnector, aeItems, parent);
            return paths;
        }

 

    }
    #endregion

    #region TrackSegment
    /// <summary>
    /// Defines a geometric Track segment.
    /// </summary>
    public class TrackSegment : GlobalItem
    {
        public string segmentLabel;
        public AESegment associateSegment;
        public bool isCurved = false;
        private bool snapped = false;
        public double lengthSegment;
        public float angle1, angle2;
        public AESectionCurve curve = null;
        public List<SideItem> sidings;
        public List<AECrossOver> aeCrossOver;
        public bool linkToOther = false;
        public StationConnector HasConnector = null;
        public MetaSegment metaSegment { get; protected set; }
        public bool Configured = false;
        [JsonIgnore]
        public StationItem parentStation { get; protected set; }


        public TrackSegment()
        {
        }

        public TrackSegment(AESegment segment, TrackNode node, int sectionID, int dir, TrackSectionsFile tdf)
        {
            metaSegment = null;
            sidings = new List<SideItem>();
            aeCrossOver = new List<AECrossOver>();
            associateSegment = segment;
            associateNode = node;
            associateNodeIdx = (int)node.Index;
            associateSectionIdx = sectionID;
            isCurved = false;
            TrackSection ts = tdf.TrackSections.Get(associateNode.TrVectorNode.TrVectorSections[associateSectionIdx].SectionIndex);
            lengthSegment = ts.SectionSize.Length;
            //lengthSegment =  associateNode.TrVectorNode.TrVectorSections[associateSectionIdx]. .SectionSize != null ? trackSection.SectionSize.Length : 0;
            CheckCurve(tdf, dir);
            associateSegment.update(isCurved, curve);
            if (node.TrVectorNode.TrItemRefs == null || node.TrVectorNode.TrItemRefs.Length <= 0)
                return;
        }

        public void AddSiding(SideItem item)
        {
            if (sidings == null || item == null)
                return;
            sidings.Add(item);
        }

        public void AddCrossOver(AECrossOver crossOver)
        {
            if (crossOver == null)
                return;
            aeCrossOver.Add(crossOver);
        }

        public void CheckCurve(TrackSectionsFile tdf, int flag2)
        {
            TrackSection ts = tdf.TrackSections.Get((uint)associateNode.TrVectorNode.TrVectorSections[associateSectionIdx].SectionIndex);
            if (ts != null)
            {
                if (ts.SectionCurve != null)
                {
                    if (ts.SectionCurve.Radius < 15f)
                        return;
                    Vector2 vectorA;
                    Vector2 vectorB;

                    vectorA = associateSegment.getStartPoint();
                    vectorB = associateSegment.getEndPoint();
                    PointF info;
                    if ((int)vectorA.X == 45892)
                        info = PointF.Empty;
                    double lenCorde = Vector2.Distance(vectorA, vectorB);
                    if (lenCorde < 2)
                        return;
                    float lenFleche = (float)((ts.SectionCurve.Radius * ts.SectionCurve.Radius) - (lenCorde * lenCorde) / 4f);
                    lenFleche = Math.Abs(lenFleche);
                    lenFleche = (float)Math.Sqrt(lenFleche);
                    lenFleche = (float)ts.SectionCurve.Radius - lenFleche;
                    if (double.IsNaN(lenFleche))
                        return;
                    curve = new AESectionCurve (ts.SectionCurve);
                    Vector3 v = new Vector3((float)(vectorB.X - vectorA.X), 0, (float)(vectorB.Y - vectorA.Y));
                    Vector3 v2 = Vector3.Cross(Vector3.Up, v);
                    v2.Normalize();
                    v = v / 2;
                    v.X += (float)vectorA.X;
                    v.Z += (float)vectorA.Y;
                    if (ts.SectionCurve.Angle < 0)
                    {
                        v = (v2 * lenFleche) + v;
                        curve.direction = 1;
                    }
                    else
                    {
                        v = (v2 * -lenFleche) + v;
                        curve.direction = -1;
                    }
                    PointF pointV = new System.Drawing.PointF((float)v.X, (float)v.Z);
                    curve.C = new MSTSCoord(pointV);
                    isCurved = true;
                    PointF pA = new PointF((float)vectorA.X, (float)vectorA.Y);
                    PointF pB = new PointF((float)vectorB.X, (float)vectorB.Y);
                    float r;
                    float errorRadius = 0;
                    if ((r = curve.setCenter(pA, pB, pointV)) != curve.Radius)
                    {
                        //File.AppendAllText(@"F:\temp\AE.txt",
                        //    "Radius original: " + curve.Radius + " ,calculé: " + r + "\n");
                        errorRadius = curve.Radius;
                        curve.Radius = r;
                        //  A surveiller!  
                    }
                    if (r != 0)
                    {
                        curve.setStartAngle(pA, pB, pointV);
                    }
                    else
                    {
                        isCurved = false;
                    }
                }
                else
                {
                    isCurved = false;
                }
            }
        }
        public int getShapeIdx()
        {
            return 0;
        }

        public bool setSnap()
        {
            snapped = true;
            return snapped;
        }

        public bool setSnaps(MSTSItems aeItems)
        {
            snapped = true;
            var indexClosest = (int)associateNodeIdx;
            foreach (var segment in aeItems.segments)
            {
                if (segment.associateNodeIdx == indexClosest)
                {
                    segment.setSnap();
                }
            }
            return snapped;
        }

        public bool setAreaSnaps(MSTSItems aeItems)
        {
            snapped = true;
            //var indexClosest = (int)associateNodeIdx;
            //foreach (var segment in mstsItems.segments)
            //{
            //    if (segment.associateNodeIdx == indexClosest && segment.inStationArea)
            //    {
            //        segment.setSnap();
            //    }
            //}
            return snapped;
        }

        public bool unsetSnap()
        {
            snapped = false;
            return snapped;
        }
        public bool isSnap()
        {
            return snapped;
        }

        public string AsString()
        {
            if (linkToOther)
                return String.Format("{0} - {1}", associateNodeIdx.ToString(), associateSectionIdx.ToString());
            return String.Format("{0} - {1}", associateNodeIdx.ToString(), associateSectionIdx.ToString());
        }

        public bool GetDecal()
        {
            return linkToOther;
        }

        public PointF getStart()
        {
            return associateSegment.startPoint;
        }

        public PointF getEnd()
        {
            return associateSegment.endPoint;
        }

        public void SetMetaSegment(MetaSegment meta)
        {
            metaSegment = meta;
        }

        public void InStation(StationItem parent)
        {
            parentStation = parent;
            setEditable();
        }

        public void OutStation()
        {
            unsetEditable();
        }
    }

    public class MetaSegment
    {
        public int direction { get; protected set; }
        public string name { get; protected set; }

        public MetaSegment()
        {
            direction = (int)AllowedDir.InOut;
        }
    }

    public class AESectionCurve : SectionCurve
    {
        public int direction;
        public int step;
        public MSTSCoord C;
        public MSTSCoord Centre = null;
        public double startAngle;
        public double angleTot;
        public List<MSTSCoord> checkedPoint = null;
        public float radiusComputed;

        public AESectionCurve()
            : base()
        {
        }

        public AESectionCurve(SectionCurve sectionCurve)
        {
            Radius = sectionCurve.Radius;
            Angle = sectionCurve.Angle;
        }

        public float setCenter(PointF startPoint, PointF endPoint, PointF middlePoint)
        {
            PointF info;
            if ((int)startPoint.X == 45892 || (int)endPoint.X == 45892 || (int)middlePoint.X == 45892)
                info = PointF.Empty;

            float t = middlePoint.X * middlePoint.X + middlePoint.Y * middlePoint.Y;
            float bc = (startPoint.X * startPoint.X + startPoint.Y * startPoint.Y - t) / 2.0f;
            float cd = (t - endPoint.X * endPoint.X - endPoint.Y * endPoint.Y) / 2.0f;
            float det = (startPoint.X - middlePoint.X) * (middlePoint.Y - endPoint.Y) - (middlePoint.X - endPoint.X) * (startPoint.Y - middlePoint.Y);

            if (Math.Abs(det) > 1.0e-6) // Determinant was found. Otherwise, radius will be left as zero.
            {
                det = 1f / det;
                float x = ((bc * (middlePoint.Y - endPoint.Y)) - (cd * (startPoint.Y - middlePoint.Y))) * det;
                float y = (((startPoint.X - middlePoint.X) * cd) - ((middlePoint.X - endPoint.X) * bc)) * det;
                radiusComputed = (float)Math.Sqrt((x - startPoint.X) * (x - startPoint.X) + (y - startPoint.Y) * (y - startPoint.Y));

                PointF current = new PointF(x, y);
                Centre = new MSTSCoord(current);
            }
            else
            {
                radiusComputed = 0;
            }
            return radiusComputed;
        }

        public void setStartAngle(PointF p1, PointF p3, PointF p2)
        {
            PointF info;
            if ((int)p1.X == 45892)
                info = PointF.Empty;
            PointF center = Centre.ConvertToPointF();

            float dx1 = p1.X - center.X;
            float dy1 = p1.Y - center.Y;

            float dx2 = p3.X - center.X;
            float dy2 = p3.Y - center.Y;

            double dx3 = p2.X - center.X;
            double dy3 = p2.Y - center.Y;

            Vector2 vector1 = new Vector2(dx1, dy1);
            Vector2 vector2 = new Vector2(dx2, dy2);
            float distBetween;
            Vector2.Distance(ref vector1, ref vector2, out distBetween);
            double atan1 = Math.Atan2(dy1, dx1);
            double atan2 = Math.Atan2(dy2, dx2);
            double atan3 = 0;
            atan3 = atan2 - atan1;
            while (atan3 > Math.PI)
                atan3 -= Math.PI;
            while (atan3 < -Math.PI)
                atan3 += Math.PI;
            if (Math.Abs(atan3) > (Math.PI / 2))
                atan3 = Math.PI - Math.Abs(atan3);
            angleTot = atan3 * 180 / Math.PI;
            startAngle = atan1 * 180 / Math.PI;
            double endAngle = atan2 * 180 / Math.PI;
            double info2x = (Radius * Math.Sin(startAngle));
            info2x = info2x + center.X;
            double info2y = (Radius * Math.Cos(startAngle));
            info2y = info2y + center.Y;

            double interm = 2;
            angleTot = atan3;
            step = (int)Math.Floor(Math.Abs(distBetween) / interm);
            startAngle = atan1;
        }

        void setCheckedPoint()
        {
            /*
            if (segment.curve.checkedPoint == null)
                segment.curve.checkedPoint = new List<MSTSCoord> ();
            else
                segment.curve.checkedPoint.Clear ();
            for (int i = 0; i < segment.curve.step; i++)
            {
                double sub_angle = ((float)i / segment.curve.step) * segment.curve.angleTot;
                double infox = (1 - Math.Cos (sub_angle)) * (-pointB.X);
                double infoy = (1 - Math.Cos (sub_angle)) * (-pointB.Y);
                double info2x = segment.curve.radiusComputed * Math.Cos (segment.curve.startAngle + sub_angle);
                double info2y = segment.curve.radiusComputed * Math.Sin (segment.curve.startAngle + sub_angle);
                double dx = pt.X - (pointCenter.X + info2x);
                double dy = pt.Y - (pointCenter.Y + info2y);
                MSTSCoord tempo = new MSTSCoord ();
                tempo.Convert (new PointF ((float)(pointCenter.X + info2x), (float)(pointCenter.Y + info2y)));
                segment.curve.checkedPoint.Add (tempo);
            }
             * */
        }
    }

    #endregion

    public class dVector
    {
        public double X, Y;
        public dVector(double x1, double y1) { X = x1; Y = y1; }
        public PointF GetPointF()
        {
            PointF val = new PointF((float)X, (float)Y);
            return val;
        }
        public Vector2 GetVector2()
        {
            Vector2 val = new Vector2((float)X, (float)Y);
            return val;
        }
        public bool CheckNaN()
        {
            if (double.IsNaN(X) || double.IsNaN(Y))
                return true;
            return false;
        }
    }

    public class edge
    {
        protected PointF A;
        protected PointF B;

        public edge(PointF a, PointF b) { A = a; B = b; }

        public edge(float aX, float aY, float bX, float bY) { A = new PointF(aX, aY); B = new PointF(bX, bY); }

    }

}
