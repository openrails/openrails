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

namespace Orts.Formats.OR
{

    public enum AllowedDir
    {
        NONE = 0,
        IN = 1,
        OUT = 2,
        InOut = 3
    };


    #region SignalItem
    /// <summary>
    /// Defines a signal being drawn in a 2D view.
    /// </summary>
    public class AESignalItem : GlobalItem
    {
        public TrItem Item;
        private List<MstsSignalFunction> sigFonction;
        public MstsSignalFunction SigFonction { get { return sigFonction.Count > 0 ? sigFonction[0] : MstsSignalFunction.UNKNOWN; } set { } }
        /// <summary>
        /// The underlying signal object as referenced by the TrItem.
        /// </summary>
        public AESignalObject Signal;

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
        /// <param name="sideItem"></param>
        /// <param name="signal"></param>
        public AESignalItem(SignalItem item, AESignalObject signal, TrackDatabaseFile TDB)
        {
            typeItem = (int)TypeItem.SIGNAL_ITEM;
            Item = item;
            Signal = signal;
            sigFonction = new List<MstsSignalFunction>();
            foreach (var sig in Signal.SignalHeads)
            {
                sigFonction.Add(sig.sigFunction);
            }
            hasDir = false;
            Location.X = item.TileX * 2048 + item.X;
            Location.Y = item.TileZ * 2048 + item.Z;
            try
            {
                associateNode = TDB.TrackDB.TrackNodes[signal.trackNode];
                Vector2 v2;
                if (associateNode.TrVectorNode != null)
                {
                    associateNodeIdx = (int)associateNode.Index;
                    var ts = associateNode.TrVectorNode.TrVectorSections[0];
                    v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z);
                }
                else if (associateNode.TrJunctionNode != null)
                {
                    associateNodeIdx = associateNode.TrJunctionNode.Idx;
                    var ts = associateNode.UiD;
                    v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z);
                }
                else
                    throw new Exception();
                var v1 = new Vector2(Location.X, Location.Y);
                var v3 = v1 - v2;
                v3.Normalize();
                v2 = v1 - Vector2.Multiply(v3, signal.direction == 0 ? 12f : -12f);
                //v2 = v1 - Vector2.Multiply(v3, 12f);
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

    #region JunctionItem
    /// <summary>
    /// Defines a junction being drawn in a 2D view.
    /// </summary>
    public class AEJunctionItem : GlobalItem
    {
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
        /// <param name="sideItem"></param>
        /// <param name="signal"></param>
        public AEJunctionItem(TrackNode item)
        {
            typeItem = (int)TypeItem.SWITCH_ITEM;
            associateNode = item;
            associateNodeIdx = item.TrJunctionNode.Idx;
            associateSectionIdx = 0;
            Location.X = associateNode.UiD.TileX * 2048f + associateNode.UiD.X;
            Location.Y = associateNode.UiD.TileZ * 2048f + associateNode.UiD.Z;
        }
    }

    #endregion

    #region CrossOver

    /// <summary>
    /// Defines a CrossOver
    /// Only for display at this level
    /// </summary>
    /// 

    public class AECrossOver : GlobalItem
    {
        public TrItem trItem { get; protected set; }
        public TrItem.trItemType type { get { return trItem.ItemType; } protected set { } }
        public float SData1 { get { return trItem.SData1; } protected set { } }
        public string SData2 { get { return trItem.SData2; } protected set { } }
        public TrackSegment CrossSegment { get; protected set; }

        public AECrossOver(TrackSegment trSegment, TrItem item)
        {
            trItem = item;
            Location = new PointF(trItem.TileX * 2048f + trItem.X, trItem.TileZ * 2048f + trItem.Z);
        }

        public void setCrossSegment(TrackSegment trSegment)
        {
            CrossSegment = trSegment;
        }
    }

    #endregion

    #region BufferItem
    public class AEBufferItem : GlobalItem
    {
        [JsonProperty("nameBuffer")]
        public string NameBuffer { get; set; }
        [JsonProperty("configured")]
        public bool Configured { get; set; }
        [JsonProperty("bufferId")]
        private int BufferId { get { return (int)associateNode.Index; } set { } }
        [JsonProperty("dirBuffer")]
        public AllowedDir DirBuffer { get; set; }
        [JsonIgnore]
        List<string> allowedDirections;
        [JsonIgnore]
        public StationPaths stationPaths { get; protected set; }
        [JsonIgnore]
        public StationItem parentStation { get; protected set; }
        [JsonConstructor]
        public AEBufferItem()
        {
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            DirBuffer = AllowedDir.NONE;
            Configured = false;
            typeItem = (int)TypeItem.BUFFER_ITEM;
            //Coord = new MSTSCoord();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sideItem"></param>
        /// <param name="signal"></param>
        public AEBufferItem(TrackNode item)
        {
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            DirBuffer = AllowedDir.NONE;
            Configured = false;
            typeItem = (int)TypeItem.BUFFER_ITEM;
            associateNode = item;
            associateNodeIdx = (int)item.Index;
            associateSectionIdx = 0;
            Coord = new MSTSCoord(item);
            Location.X = associateNode.UiD.TileX * 2048f + associateNode.UiD.X;
            Location.Y = associateNode.UiD.TileZ * 2048f + associateNode.UiD.Z;
        }

        public override void alignEdition(TypeEditor interfaceType, GlobalItem ownParent)
        {
            if (interfaceType == TypeEditor.ROUTECONFIG)
            {
                setEditable();
                asMetadata = true;
            }
            parentStation = (StationItem)ownParent;
        }

        public List<StationPath> searchPaths(AETraveller myTravel, List<TrackSegment> listConnectors, MSTSItems aeItems, StationItem parent)
        {
            List<StationPath> paths;
            if (!Configured)
                return null;
            if (stationPaths == null)
            {
                stationPaths = new StationPaths();
            }
            stationPaths.Clear();
            paths = stationPaths.explore(myTravel, listConnectors, aeItems, parent);
            return paths;
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            if (!Configured)
                return;

            if (stationPaths != null)
            {
                stationPaths.highlightTrackFromArea(aeItems);
            }
        }

        public List<string> getAllowedDirections()
        {
            return allowedDirections;
        }

        public AllowedDir getDirBuffer()
        {
            return DirBuffer;
        }
        public void setDirBuffer(string info)
        {
            if (info == allowedDirections[1])
                DirBuffer = AllowedDir.IN;
            else if (info == allowedDirections[2])
                DirBuffer = AllowedDir.OUT;
            else if (info == allowedDirections[3])
                DirBuffer = AllowedDir.InOut;
            else
                DirBuffer = AllowedDir.NONE;
        }

        public void updateNode(TrackNode node)
        {
            allowedDirections = new List<string>();
            allowedDirections.Add("   ");
            allowedDirections.Add("In");
            allowedDirections.Add("Out");
            allowedDirections.Add("Both");
            //DirBuffer = AllowedDir.NONE;
            if (NameBuffer != null && NameBuffer.Length > 0 && DirBuffer != AllowedDir.NONE)
                Configured = true;
            else
                Configured = false;
            typeItem = (int)TypeItem.BUFFER_ITEM;
            associateNode = node;
            //associateNodeIdx = (int)componentItem.Index;
            associateSectionIdx = 0;
            Coord = new MSTSCoord(node);
            Location.X = associateNode.UiD.TileX * 2048f + associateNode.UiD.X;
            Location.Y = associateNode.UiD.TileZ * 2048f + associateNode.UiD.Z;
            node.Reduced = true;
        }
    }
    #endregion

    #region SideItem

    /// <summary>
    /// Defines a siding sideItem  (platform, difing or passing)
    /// SideStartItem is the place where the Siding Label is attached
    /// SideEndItem is the end place
    /// </summary>
    /// 
    public class SideItem : GlobalItem
    {
        public string Name;
        public float sizeSiding;
        public TrItem trItem{ get; protected set; }
        public TrItem.trItemType type { get { return trItem.ItemType; } protected set { } }
        public float icoAngle;
        public int typeSiding;
    }

    public class SideStartItem : SideItem
    {
        public SideEndItem endSiding { get; set; }
       
        /// <summary>
        /// The underlying track sideItem.
        /// </summary>

        /// <param name="sideItem"></param>
        /// <param name="signal"></param>
        public SideStartItem(TrackSegment trSegment, TrItem item)
        {
            if (item.ItemType == TrItem.trItemType.trPLATFORM)
                typeSiding = (int)TypeSiding.PLATFORM_START;
            else if (item.ItemType == TrItem.trItemType.trSIDING)
                typeSiding = (int)TypeSiding.SIDING_START;
            typeItem = (int)TypeItem.SIDING_START;
            
            trItem = item;
            Name = trItem.ItemName;
            Location = new PointF(trItem.TileX * 2048f + trItem.X, trItem.TileZ * 2048f + trItem.Z);
            type = trItem.ItemType;
            sizeSiding = item.SData1;
        }
    }

    public class SideEndItem : SideItem
    {
        public SideStartItem startSiding { get; set; }
        /// <summary>
        /// The underlying track sideItem.
        /// </summary>

        public SideEndItem(TrackSegment trSegment, TrItem item, SideStartItem siding)
        {
            if (siding == null)
                return;
            typeItem = siding.typeItem;
            type = siding.trItem.ItemType;
            startSiding = siding;
            if (siding.sizeSiding != item.SData1)
                sizeSiding = item.SData1 - siding.sizeSiding;
            typeSiding = (int)TypeSiding.SIDING_END;
            trItem = item;
            Location = new PointF(trItem.TileX * 2048f + trItem.X, trItem.TileZ * 2048f + trItem.Z);
        }
    }
    #endregion

    #region ShapeItem
    /// <summary>
    /// Defines a siding name being drawn in a 2D view.
    /// Work in Progress
    /// </summary>
    public class ShapeItem : GlobalItem
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
        /// <param name="sideItem"></param>
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

}
