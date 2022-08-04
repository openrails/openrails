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
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
//using Microsoft.Xna.Framework;
using XnaGeometry;
using MSTS;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS;

using ORTS;
using LibAE;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace LibAE
{
    [Flags]
    enum TypeWidget
    {
        ITEM_WIDGET = 0,
        SIGNAL_WIDEGT = 1,
        SWITCH_WIDGET = 2,
        TAG_WIDGET = 3,
        BUFFER_WIDGET = 4,
        SIDING_WIDGET = 5,
        SIGNAL_WIDGET = 6,
        STATION_WIDGET = 7,
        STATION_AREA_WIDGET = 8,
        STATION_INTERFACE = 9,
        ACTIVITY_WIDGET = 10
    };

    public enum AllowedDir
    {
        NONE = 0,
        IN = 1,
        OUT = 2,
        InOut = 3
    };

    public class AEWidget

    {

        [JsonProperty("WidgetsName")]
        string aeWidgetName;
        [JsonIgnore]
        public List<AESwitchItem> switches;
        [JsonIgnore]
        public List<AESignalItem> signals;
        [JsonIgnore]
        public List<AESidingItem> sidings;
        [JsonIgnore]
        public List<AEBufferItem> buffers;
        [JsonIgnore]
        public List<TrackSegment> segments;
        [JsonIgnore]
        public List<ShapeItem> shapes;

        public AEWidget()
        {
            aeWidgetName = "coco";
            signals = new List<AESignalItem>();
            sidings = new List<AESidingItem>();
            switches = new List<AESwitchItem>();
            buffers = new List<AEBufferItem>();
            segments = new List<TrackSegment>();
            shapes = new List<ShapeItem>();
        }


        public void AddSegment(TrackSegment line)
        {
            PointF lineA = line.A.ConvertToPointF();
            PointF lineB = line.B.ConvertToPointF();
            if (lineA == lineB)
                return;
            foreach (var segment in segments)
            {
                PointF segmentA = segment.A.ConvertToPointF();
                PointF segmentB = segment.B.ConvertToPointF();
                if (lineA == segmentA && lineB == segmentB)
                    return;
                if (lineA == segmentB && lineB == segmentA)
                    return;
            }
            segments.Add(line);
        }

        public bool saveConfig(StreamWriter wr)
        {

            return false;

        }

        public static AEWidget loadConfig(StreamReader rd)
        {

            return null;

        }
    }

    #region globalItem


    public class globalItem
    {
        [JsonProperty("Location")]
        public PointF Location;
        [JsonProperty("Location2D")]
        public PointF Location2D;
        [JsonProperty("typeWidget")]
        public int typeWidget;
        [JsonProperty("CoordMSTS")]
        public MSTSCoord Coord;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public globalItem()
        {
            movable = false;
            rotable = false;
            editable = false;
            lineSnap = false;
            actEdit = false;
            isSeen = false;
            typeWidget = (int)TypeWidget.ITEM_WIDGET;
            Location = new PointF(float.NegativeInfinity, float.NegativeInfinity);
            Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
        }

        public virtual void alignEdition(TypeEditor interfaceType, globalItem ownParent) { }

        public virtual void configCoord(MSTSCoord coord)
        {
            Coord = coord;
            Location.X = coord.TileX * 2048f + coord.X; 
            Location.Y = coord.TileY * 2048f + coord.Y;
        }

        public virtual void Update(MSTSCoord coord, TrackSegment segment)
        {
        }

        public virtual void SynchroLocation()
        {
        }

        public virtual double FindItem(PointF point, double snap, double actualDist)
        {
            double distD;
            isSeen = false;
            if ((Location.X < point.X - snap) || (Location.X > point.X + snap)
                || (Location.Y < point.Y - snap) || (Location.Y > point.Y + snap))
            {
                return double.PositiveInfinity;
            }
            distD = Math.Sqrt(Math.Pow((Location.X - point.X), 2) + Math.Pow((Location.Y - point.Y), 2));
            if (distD > actualDist)
                return double.PositiveInfinity;
            isSeen = true;
            return distD;
        }

        public virtual void complete(ORRouteConfig orRouteConfig, List<TrackSegment> segments, MSTSBase tileBase) { }
        public bool isItSeen() { return isSeen; }
        public virtual void Edit() { }

        public virtual void setAngle(float angle) { }

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
    }
    #endregion

    #region SignalItem
    /// <summary>
    /// Defines a signal being drawn in a 2D view.
    /// </summary>
    public class AESignalItem : globalItem
    {
        public TrItem Item;
        private List<SignalHead.MstsSignalFunction> sigFonction;
        public SignalHead.MstsSignalFunction SigFonction { get { return sigFonction.Count > 0 ? sigFonction[0] : SignalHead.MstsSignalFunction.UNKNOWN; } set { } }
        /// <summary>
        /// The underlying signal object as referenced by the TrItem.
        /// </summary>
        public SignalObject Signal;

        public PointF Dir;
        public bool hasDir = false;
        /// <summary>
        /// For now, returns true if any of the signal heads shows any "clear" aspect.
        /// This obviously needs some refinement.
        /// </summary>
        public int IsProceed
        {
            get
            {
                int returnValue = 2;

                foreach (var head in Signal.SignalHeads)
                {
                    if (head.state == MstsSignalAspect.CLEAR_1 ||
                        head.state == MstsSignalAspect.CLEAR_2)
                    {
                        returnValue = 0;
                    }
                    if (head.state == MstsSignalAspect.APPROACH_1 ||
                        head.state == MstsSignalAspect.APPROACH_2 || head.state == MstsSignalAspect.APPROACH_3)
                    {
                        returnValue = 1;
                    }
                }

                return returnValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public AESignalItem(SignalItem item, SignalObject signal, TDBFile TDB)
        {
            typeWidget = (int)TypeWidget.SIGNAL_WIDGET;
            Item = item;
            Signal = signal;
            sigFonction = new List<SignalHead.MstsSignalFunction>();
            foreach (var sig in Signal.SignalHeads)
            {
                sigFonction.Add(sig.sigFunction);
                if (sig.sigFunction == SignalHead.MstsSignalFunction.SPEED)
                    File.AppendAllText(@"C:\temp\AE.txt", "SPEED\n");
            }
            foreach (var fn in sigFonction)
            {
                File.AppendAllText(@"C:\temp\AE.txt", "FN " + fn + "\n");
            }
            hasDir = false;
            Location.X = item.TileX * 2048 + item.X; 
            Location.Y = item.TileZ * 2048 + item.Z;
            try
            {
                var node = TDB.TrackDB.TrackNodes[signal.trackNode];
                Vector2 v2;
                if (node.TrVectorNode != null) 
                { 
                    var ts = node.TrVectorNode.TrVectorSections[0]; 
                    v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z); 
                }
                else if (node.TrJunctionNode != null) 
                { 
                    var ts = node.UiD; 
                    v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z); 
                }
                else 
                    throw new Exception();
                var v1 = new Vector2(Location.X, Location.Y); 
                var v3 = v1 - v2; 
                v3.Normalize(); 
                //v2 = v1 - Vector2.Multiply(v3, signal.direction == 0 ? 12f : -12f);
                v2 = v1 - Vector2.Multiply(v3, 12f);
                Dir.X = (float)v2.X;
                Dir.Y = (float)v2.Y;
                //v2 = v1 - Vector2.Multiply(v3, signal.direction == 0 ? 1.5f : -1.5f);//shift signal along the dir for 2m, so signals will not be overlapped
                v2 = v1 - Vector2.Multiply(v3, 1.5f);
                Location.X = (float)v2.X;
                Location.Y = (float)v2.Y;
                hasDir = true;
            }
            catch { }
        }
    }
    #endregion

    #region SwitchWidget
    /// <summary>
    /// Defines a signal being drawn in a 2D view.
    /// </summary>
    public class AESwitchItem : globalItem
    {
        public TrackNode Item;
        public uint main;
#if SPA_ADD
            public string Name;
#endif
#if false
           public dVector mainEnd = null;
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public AESwitchItem(TrackNode item)
        {
            typeWidget = (int)TypeWidget.SWITCH_WIDGET;
            Item = item;
#if SPA_ADD
                Name = item.Index.ToString();    //  SPA
#endif
            /*
            var TS = Program.Simulator.TSectionDat.TrackShapes.Get(item.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route

            if (TS != null) { main = TS.MainRoute; }
            else main = 0;
#if false
	           try
	           {
		           var pin = item.TrPins[1];
		           TrVectorSection tn;

		           if (pin.Direction == 1) tn = Program.Simulator.TDB.TrackDB.TrackNodes[pin.Link].TrVectorNode.TrVectorSections.First();
		           else tn = Program.Simulator.TDB.TrackDB.TrackNodes[pin.Link].TrVectorNode.TrVectorSections.Last();

		           if (tn.SectionIndex == TS.SectionIdxs[TS.MainRoute].TrackSections[0]) { mainEnd = new dVector(tn.TileX * 2048 + tn.X, tn.TileZ * 2048 + tn.Z); }
		           else
		           {
			           var pin2 = item.TrPins[2];
			           TrVectorSection tn2;

			           if (pin2.Direction == 1) tn2 = Program.Simulator.TDB.TrackDB.TrackNodes[pin2.Link].TrVectorNode.TrVectorSections.First();
			           else tn2 = Program.Simulator.TDB.TrackDB.TrackNodes[pin2.Link].TrVectorNode.TrVectorSections.Last();
			           if (tn2.SectionIndex == TS.SectionIdxs[TS.MainRoute].TrackSections[0]) { mainEnd = new dVector(tn.TileX * 2048 + tn.X, tn.TileZ * 2048 + tn.Z); }
		           }
        		  
	           }
	           catch { mainEnd = null; }
#endif
            */
            Location.X = Item.UiD.TileX * 2048f + Item.UiD.X; 
            Location.Y = Item.UiD.TileZ * 2048f + Item.UiD.Z;
        }
    }

    #endregion

    #region BufferWidget
    public class AEBufferItem : globalItem
    {
        public TrackNode Item;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public AEBufferItem(TrackNode item)
        {
            typeWidget = (int)TypeWidget.BUFFER_WIDGET; ;
            Item = item;

            Location.X = Item.UiD.TileX * 2048f + Item.UiD.X; 
            Location.Y = Item.UiD.TileZ * 2048f + Item.UiD.Z;
        }
    }
    #endregion

    #region TagWidget

#if !JSON_OR_XML
    [Serializable()]
#else
#endif
    public class TagItem : globalItem
    {
        [JsonProperty("nameTag")]
        public string nameTag { get; set; }
        [JsonProperty("nameVisible")]
        public bool nameVisible;

        public TagItem(TypeEditor interfaceType)
        {
            alignEdition(interfaceType, null);
        }

        public override void alignEdition(TypeEditor interfaceType, globalItem ownParent)
        { 
            setMovable();
        }

        public void setNameTag(int info)
        {
            nameTag = "tag" + info;
        }

        public override void configCoord (MSTSCoord coord)
        {
            base.configCoord(coord);
            typeWidget = (int)TypeWidget.TAG_WIDGET;
            nameVisible = false;
        }

        public override void Update(MSTSCoord coord, TrackSegment segment)
        {
            base.configCoord(coord);
        }
    }
    #endregion

    #region StationWidget

#if !JSON_OR_XML
    [Serializable()]
#else
#endif
    public class StationItem : globalItem
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

        public StationItem(TypeEditor interfaceType)
        {
            icoAngle = 0f;
            stationArea = new List<StationAreaItem>();
            alignEdition(interfaceType, null);
            areaCompleted = false;
            
        }

        public override void alignEdition(TypeEditor interfaceType, globalItem ownParent)
        {
            if (interfaceType == TypeEditor.ROUTECONFIG)
            {
                setMovable();
                setRotable();
            }
        }

        public void setNameStation(int info)
        {
            nameStation = "station" + info;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
            typeWidget = (int)TypeWidget.STATION_WIDGET;
            nameVisible = false;
        }

        public List<StationAreaItem> getStationArea()
        {
            return stationArea;
        }

        public StationAreaItem getNextArea(StationAreaItem cur)
        {
            int i;
            if (cur == null && stationArea.Count() <= 0)
                return null;
            for (i = 0; i < stationArea.Count(); i++)
            {
                if (stationArea[i] == cur)
                    break;
            }
            if (i > stationArea.Count())
                return stationArea[0];
            return stationArea[i];
        }

        public override void Update(MSTSCoord coord, TrackSegment segment)
        {
            base.configCoord(coord);
        }

        public override void setAngle(float angle)
        {
            icoAngle = angle;
        }

        public StationAreaItem AddPointArea(MSTSCoord coord, double snapDist, MSTSBase tileBase)
        {
            PointF found = new PointF(0, 0);
            double dist = -1, closestDist = double.PositiveInfinity;
            int i;
            int closestI = -1;
            PointF closest;

            StationAreaItem newPoint = new StationAreaItem(TypeEditor.ROUTECONFIG, this);

            if (areaCompleted)  
            {   
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
                        closestI = i+1;
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

        public override void complete(ORRouteConfig orRouteConfig, List<TrackSegment> segments, MSTSBase tileBase)
        {
            if (stationArea.Count > 0)
            {
                checkForNewConnector(orRouteConfig, segments, tileBase);
                areaCompleted = true;
            }
        }

        public List<System.Drawing.PointF> getPolyPoints() //  Or Polygone, it's the same
        {
            List<System.Drawing.PointF> polyPoints = new List<System.Drawing.PointF>();
            foreach (StationAreaItem SAWidget in stationArea)
            {
                float X = (SAWidget.Coord.TileX * 2048f + SAWidget.Coord.X);
                float Y = SAWidget.Coord.TileY * 2048f + SAWidget.Coord.Y;
                polyPoints.Add(new System.Drawing.PointF(X, Y));
            }
            return polyPoints;
        }

        public List<AESegment> getPolySegment() //  List of perimeter's segments for this area
        {
            StationAreaItem SAItem1;
            StationAreaItem SAItem2;
            PointF startSegment;
            PointF endSegment;

            if (stationArea.Count <= 0)
                return null;
            List<AESegment> polySegments = new List<AESegment> ();
            for (int cnt = 0; cnt < stationArea.Count -1; cnt++)
            {
                SAItem1 = stationArea[cnt];
                SAItem2 = stationArea[cnt+1];
                startSegment = new PointF((SAItem1.Coord.TileX * 2048f + SAItem1.Coord.X), (SAItem1.Coord.TileY * 2048f + SAItem1.Coord.Y));
                endSegment = new PointF((SAItem2.Coord.TileX * 2048f + SAItem2.Coord.X), (SAItem2.Coord.TileY * 2048f + SAItem2.Coord.Y));
                polySegments.Add(new AESegment (startSegment, endSegment));
            }
            SAItem1 = stationArea[0];
            SAItem2 = stationArea[stationArea.Count - 1];
            startSegment = new PointF ((SAItem1.Coord.TileX * 2048f + SAItem1.Coord.X), (SAItem1.Coord.TileY * 2048f + SAItem1.Coord.Y));
            endSegment = new PointF ((SAItem2.Coord.TileX * 2048f + SAItem2.Coord.X), (SAItem2.Coord.TileY * 2048f + SAItem2.Coord.Y));
            polySegments.Add (new AESegment (endSegment, startSegment));

            return polySegments;
        }

        public override double FindItem(PointF point, double snap, double actualDist)
        {
            double iconDist = double.PositiveInfinity;
            List<System.Drawing.PointF> poly = getPolyPoints();
            int i, j = poly.Count - 1;
            bool oddNodes = false;
            double dist = double.PositiveInfinity;

            isSeen = false;
            if (!((Location.X < point.X - snap) || (Location.X > point.X + snap)
                || (Location.Y < point.Y - snap) || (Location.Y > point.Y + snap)))
            {
                isSeen = true;
                iconDist =  (Math.Sqrt(Math.Pow((Location.X - point.X), 2) + Math.Pow((Location.Y - point.Y), 2)));
            }

            for (i = 0; i < poly.Count; i++)
            {
                dist = stationArea[i].FindItem(point, snap, iconDist < actualDist?iconDist:actualDist);
                if (stationArea[i].isSeen)
                {
                    isSeen = false;
                    return dist;
                }
                if ((poly[i].Y < point.Y && poly[j].Y >= point.Y ||
                    poly[j].Y < point.Y && poly[i].Y >= point.Y)
                && (poly[i].X <= point.X || poly[j].X <= point.X))
                {
                    oddNodes ^= (poly[i].X + (point.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < point.X);
                }
                j = i;
            }

            if (oddNodes)
            {
                return 0;
            }
            return iconDist;
        }

        public void checkForNewConnector(ORRouteConfig orRouteConfig, List<TrackSegment> segments, MSTSBase tileBase)
        {
            PointF pointIntersect;
            int realPosit = 0;
            int ii = 0;

            //  First, we remove all un configured connectors
            //foreach (StationAreaItem SAWidget in stationArea)
            for (int cnt = 0; cnt < stationArea.Count; cnt++)
            {
                var SAWidget = stationArea[cnt];
                if (SAWidget.IsInterface() && !SAWidget.getStationConnector().isConfigured())
                {
                    removeConnector(orRouteConfig, SAWidget);
                    cnt--;
                }
            }
            //  Next, we search for new connectors and add them
            List<AESegment> polySegment = getPolySegment();
            for (int cntPointEdge = 0; cntPointEdge < polySegment.Count; cntPointEdge++)
            {
                List<System.Drawing.PointF> pointsIntersect = new List<System.Drawing.PointF>();
                foreach (var segment in segments)
                {
                    if ((segment.NodeIdx == 35 || segment.NodeIdx == 86) && cntPointEdge > 2 && 
                        (segment.SectionIdxA == 191 || segment.SectionIdxA == 40000))
                        ii = 1;
                    pointIntersect = DrawUtility.FindIntersection(polySegment[cntPointEdge], new AESegment(segment));
                    if (!pointIntersect.IsEmpty)
                    {
                        
                        StationAreaItem newPoint = new StationAreaItem(TypeEditor.ROUTECONFIG, this);
                        MSTSCoord coord = tileBase.getMstsCoord(pointIntersect);
                        newPoint.configCoord(coord);
                        //newPoint.toggleSelected();
                        realPosit++;    // +1 car on doit placer après le cntEdge courant
                        List<AESegment> newPolySegment = getPolySegment ();
                        insertConnector (newPolySegment, newPoint);
                        newPoint.DefineAsInterface(segment);
                    }
                }
                realPosit++;    //  On incrémente pour suivre le cntPointEdge
            }
        }

        //  Permet d'insérer un connecteur entre deux sommets de l'area
        void insertConnector(List<AESegment> polySegment, StationAreaItem connector)
        {
            for (int cnt = 0; cnt < polySegment.Count; cnt++)
            {
                if (polySegment[cnt].PointOnSegment (connector.Location))
                    stationArea.Insert (cnt+1, connector);
            }
        }

        public void removeConnector(ORRouteConfig orRouteConfig, StationAreaItem connector)
        {
            stationArea.Remove(connector);
            orRouteConfig.RemoveConnectorWidget(connector);
        }
    }
    #endregion

    #region StationAreaItem

#if !JSON_OR_XML
    [Serializable()]
#else
#endif
    public class StationAreaItem : globalItem
    {
        [JsonProperty("Connector")]
        StationConnector stationConnector = null;
        [JsonIgnore]
        bool selected = false;
        [JsonIgnore]
        public StationItem parent { get; protected set; }

        public StationAreaItem(TypeEditor interfaceType, StationItem myParent)
        {
            alignEdition(interfaceType, myParent);
            parent = myParent;
        }

        public override void alignEdition(TypeEditor interfaceType, globalItem ownParent)
        {
            if (interfaceType == TypeEditor.ROUTECONFIG)
                setMovable();
            if (parent == null)
                parent = (StationItem)ownParent;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public void DefineAsInterface(TrackSegment segment)
        {
            typeWidget = (int)TypeWidget.STATION_INTERFACE;
            if (stationConnector == null)
            {
                stationConnector = new StationConnector(segment);
            }
            stationConnector.Init(segment);
            setLineSnap();
            setEditable();
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

        public override void Update(MSTSCoord coord, TrackSegment segment)
        {   
            base.configCoord(coord);
        }

        public bool IsInterface()
        {
            if (typeWidget == (int)TypeWidget.STATION_INTERFACE)
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
    }

#if !JSON_OR_XML
    [Serializable()]
#else
#endif
    public class StationConnector
    {
        [JsonProperty("dirConnector")]
        AllowedDir dirConnector;
        [JsonProperty("labelConnector")]
        string label = "";
        List<string> allowedDirections;
        [JsonIgnore]
        TrackSegment segment;
        [JsonPropertyAttribute("angle")]
        public float angle;
        [JsonProperty("IdxMaster")]
        public uint idxMaster;
        [JsonProperty("IdxSecond")]
        public uint idxSecond;
        [JsonProperty("Configured")]
        bool configured;
        [JsonIgnore]
        TrackSegment parentSegment;


        [JsonConstructor]
        public StationConnector(int i)
        {
            File.AppendAllText(@"F:\temp\AE.txt", "Json StationConnector :" + (int)i + "\n");
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            dirConnector = AllowedDir.NONE;
            configured = false;
        }

        public StationConnector(TrackSegment segment)
        {
            File.AppendAllText(@"F:\temp\AE.txt", "StationConnector\n");
            parentSegment = segment;
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            dirConnector = AllowedDir.NONE;
            configured = false;
        }

        public void Init(TrackSegment info)
        {
            segment = info;
            idxMaster = info.SectionIdxA;
            idxSecond = info.SectionIdxB;
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
            PointF end1 = new System.Drawing.PointF((float)segment.A.X, (float)segment.A.Y);
            PointF end2 = new System.Drawing.PointF((float)segment.B.X, (float)segment.B.Y);

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

    }
    #endregion

    #region SidingItem

    /// <summary>
    /// Defines a siding name being drawn in a 2D view.
    /// </summary>
    public class AESidingItem : globalItem
    {
        public PointF Location2;
        public string Name;
        public float sizeSiding;
        public TrItem.trItemType type;
        public float icoAngle;
        /// <summary>
        /// The underlying track item.
        /// </summary>
        private TrItem Item1 = null;
        private TrItem Item2 = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public AESidingItem(TrItem item)
        {
            typeWidget = (int)TypeWidget.SIDING_WIDGET;
            if (Item1 == null)
            {
                Item1 = item;
                Name = Item1.ItemName;
                Location = new PointF(Item1.TileX * 2048f + Item1.X, Item1.TileZ * 2048f + Item1.Z);
                type = Item1.ItemType;
            }
            else
            {
                Item2 = item;

            }
            sizeSiding = 0;
        }

        public void setItem2(TrItem tr)
        {
            Item2 = tr;
            Location2 = new PointF(Item2.TileX * 2048f + Item2.X, Item2.TileZ * 2048f + Item2.Z);
        }

    }
    #endregion

    #region ShapeWidget
    /// <summary>
    /// Defines a siding name being drawn in a 2D view.
    /// </summary>
    public class ShapeItem : globalItem
    {
        public dVector A;
        public dVector B;
        public dVector C;
        //public float radius = 0.0f;
        public bool isCurved = false;
        public int ShapeIdx;
        public List<TrackSegment> trItems;
        public bool junction = false;
        public SectionCurve curve = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public ShapeItem(int idx)
        {
            ShapeIdx = idx;
            trItems = new List<TrackSegment>();
        }

        public void addTrItem(TrackSegment tr)
        {
            if (tr != null)
                trItems.Add(tr);
        }

        public List<TrackSegment> getTrItems()
        {
            return trItems;
        }

        public void setJunction()
        {
            junction = true;
        }

        public bool isJunction()
        {
            return junction;
        }
    }

    #endregion

    #region TrackSegment
    /// <summary>
    /// Defines a geometric line segment.
    /// </summary>
    public class TrackSegment : globalItem
    {
        //public TrackNodeAE NodeIdxA;
        public uint NodeIdx;
        public uint SectionIdxA;
        public MSTSCoord A;
        public TrackNodeAE NodeIdxB;
        public uint SectionIdxB;
        public MSTSCoord B;
        //public MSTSCoord C;
        //public float radius = 0.0f;
        public bool isCurved = false;
        private bool snapped = false;
        
        public uint WorldUid;

        public float angle1, angle2;
        public AESectionCurve curve = null;
        //public MSTSCoord Centre = null;

        public bool linkToOther = false;
        public List<int> tcIndex;

        public TrackSegment()
        {
        }

        public TrackSegment(TrackNodeAE nodeA, TrackNodeAE nodeB, TSectionDatFile tdf, int i)
        {
            tcIndex = new List<int>();

            int direction = DrawUtility.getDirection(nodeA, nodeB);
            A = nodeA.getMSTSCoord(direction);
            B = nodeB.getMSTSCoord(direction);
            NodeIdx = nodeA.Index;
            SectionIdxB = (uint)i;
            if (nodeA.TrJunctionNode != null)
            {
                direction = DrawUtility.getDirection(nodeB, nodeA);
                SectionIdxA = nodeB.getSectionIndex(direction);
                if (nodeB.TCCrossReference != null)
                {
                    foreach (var tcc in nodeB.TCCrossReference)
                    {
                        tcIndex.Add(tcc.CrossRefIndex);
                    }
                }
            }
            else
            {
                SectionIdxA = nodeA.getSectionIndex(direction);
                if (nodeA.TCCrossReference != null)
                {
                    foreach (var tcc in nodeA.TCCrossReference)
                    {
                        tcIndex.Add(tcc.CrossRefIndex);
                    }
                }
            }
            isCurved = false;
            WorldUid = nodeA.getWorldFileUiD();
            CheckCurve(tdf, direction);
            linkToOther = true;
        }

        public TrackSegment(TrackNodeAE node, int idx, TSectionDatFile tdf)
        {
            tcIndex = new List<int>();
            if (node.TCCrossReference != null)
            {
                foreach (var tcc in node.TCCrossReference)
                {
                    tcIndex.Add(tcc.CrossRefIndex);
                }
            }
            TrVectorSection item1 = node.TrVectorNode.TrVectorSections[idx];
            TrVectorSection item2 = node.TrVectorNode.TrVectorSections[idx+1];
            A = new MSTSCoord();
            A.TileX = item1.TileX;
            A.TileY = item1.TileZ;
            A.X = item1.X;
            A.Y = item1.Z;
            B = new MSTSCoord();
            SectionIdxA = node.TrVectorNode.TrVectorSections[idx].SectionIndex;
            B.TileX = item2.TileX;
            B.TileY = item2.TileZ;
            B.X = item2.X;
            B.Y = item2.Z;
            NodeIdx = node.Index;
            SectionIdxB = (uint)idx+1;
            isCurved = false;
            WorldUid = node.getWorldFileUiD();
            CheckCurve(tdf, item1.flag2);
        }

        public void CheckCurve(TSectionDatFile tdf, int flag2)
        {
            TrackSection ts = tdf.TrackSections.Get((uint)SectionIdxA);
            if (ts != null)
            {
                if (ts.SectionCurve != null)
                {
                    Vector2 vectorA;
                    Vector2 vectorB;
                    if (ts.SectionCurve.Radius < 15f)
                        return;
                    vectorA = A.ConvertVector2();
                    vectorB = B.ConvertVector2();
                    double lenCorde = Vector2.Distance(vectorA, vectorB);
                    if (lenCorde < 2)
                        return;
                    double lenFleche = ((ts.SectionCurve.Radius * ts.SectionCurve.Radius) - (lenCorde * lenCorde) / 4f);
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
                    curve.C = new MSTSCoord();
                    curve.C.Convert(pointV);
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
                        //  A surveiller!  
                    }
                    else
                    {
                        curve.setStartAngle(pA, pB, pointV);
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
                return String.Format("{0} - {1}", NodeIdx.ToString(), SectionIdxB.ToString());
            return String.Format("{0} - {1}", NodeIdx.ToString(), SectionIdxB.ToString());
        }

        public bool GetDecal()
        {
            return linkToOther;
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

        public float setCenter(PointF p1, PointF p3, PointF p2)
        {
            float t = p2.X * p2.X + p2.Y * p2.Y;
            float bc = (p1.X * p1.X + p1.Y * p1.Y - t) / 2.0f;
            float cd = (t - p3.X * p3.X - p3.Y * p3.Y) / 2.0f;
            float det = (p1.X - p2.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p2.Y);

            if (Math.Abs(det) > 1.0e-6) // Determinant was found. Otherwise, radius will be left as zero.
            {
                det = 1f / det;
                float x = ((bc * (p2.Y - p3.Y)) - (cd * (p1.Y - p2.Y))) * det;
                float y = (((p1.X - p2.X) * cd) - ((p2.X - p3.X) * bc)) * det;
                radiusComputed = (float)Math.Sqrt((x - p1.X) * (x - p1.X) + (y - p1.Y) * (y - p1.Y));

                PointF current = new PointF(x, y);
                Centre = new MSTSCoord();
                Centre.Convert(current);
            }
            else
            {
                radiusComputed = 0;
            }
            return radiusComputed;
        }

        public void setStartAngle(PointF p1, PointF p3, PointF p2)
        {
            PointF center = Centre.ConvertToPointF();

            float dx1 = p1.X - center.X;
            float dy1 = p1.Y - center.Y;

            float dx2 = p3.X - center.X;
            float dy2 = p3.Y - center.Y;

            double dx3 = p2.X - center.X;
            double dy3 = p2.Y - center.Y;

            Vector2 vector1 = new Vector2(dx1, dy1);
            Vector2 vector2 = new Vector2(dx2, dy2);
            double distBetween;
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
            angleTot = atan3;
            double interm = 2;
            step = (int)Math.Floor(Math.Abs(distBetween) / interm)+1;
            startAngle = atan1;
        }

        void setCheckedPoint ()
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
