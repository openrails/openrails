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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ActivityEditor.Route_Metadata;
using LibAE.Formats;
using Orts.Formats.OR;
using ORTS.Common;

namespace ActivityEditor.Engine
{
    /// <summary>
    /// Used to manage the right side panel on the main windows.
    /// </summary>
    public class AEConfig
    {
        // Fields
        public AEActivity aeActivity;
        private GroupBox AEConfigPanel;
        private GroupBox routeData;
        public List<GroupBox> stationGBList;
        public List<StationWidgetInfo> stationWidgetInfo;
        public List<GroupBox> tagGBList;
        public List<TagWidgetInfo> tagWidgetInfo;
        private TypeEditor typeConfig;
        // Properties

        public AERouteConfig aeRouteConfig { get; set; }
        public Viewer2D Viewer { get; set; }
        public PseudoSim simulator { get { return Viewer.Simulator; } protected set { } }
        public MSTSItems aeItems { get { return simulator.mstsItems; } protected set { } }
        Random rnd = new Random();

        // Methods
        public AEConfig(TypeEditor type, ActEditor panel, Viewer2D viewer)
        {
            routeData = panel.routeData;
            AEConfigPanel = panel.activityOverview;
            typeConfig = type;
            Viewer = viewer;
            aeRouteConfig = new AERouteConfig(this);
            if (typeConfig == TypeEditor.ACTIVITY)
            {
                aeActivity = new AEActivity();
            }
        }

        public void AddActItem(GlobalItem item)
        {
            aeActivity.AddActItem(item);
        }

        public void AddORItem(GlobalItem item)
        {
            aeRouteConfig.orRouteConfig.AddItem(item);
        }

        public StationAreaItem AddPointArea(StationItem itemToUpdate, MSTSCoord coord)
        {
            PointF tf = new PointF(0f, 0f);
            return itemToUpdate.AddPointArea(coord, Viewer.snapSize, Viewer.Simulator.mstsDataConfig.TileBase);
        }

        public void AddStationInfo(StationWidgetInfo info)
        {
            stationWidgetInfo.Add(info);
            stationGBList.Add(info.getStation());
            aeRouteConfig.AddStationPanel(stationGBList.ToArray());
        }

        public void AddTagWidgetInfo(TagWidgetInfo info)
        {
            tagWidgetInfo.Add(info);
            tagGBList.Add(info.getTag());
            aeRouteConfig.AddTagPanel(tagGBList.ToArray());
        }

        public void Close(TypeEditor viewerMode)
        {
            aeRouteConfig.CloseRoute();
            routeData.Visible = false;
            if (viewerMode == TypeEditor.ACTIVITY)
            {
                aeActivity.ClosegActivity();
                AEConfigPanel.Visible = false;
            }
        }

        public GlobalItem EditItem(GlobalItem item)
        {
            GlobalItem edited = null;
            if (item.IsEditable() && (typeof(StationAreaItem) == item.GetType()))
            {
                ((StationAreaItem)item).setInterfaceConfigured();
                StationInterface interface2 = new StationInterface(((StationAreaItem)item).getStationConnector());
                interface2.ShowDialog();
                if (interface2.ToRemove)
                {
                    return null;
                }
                item.Edit();
                edited = ((StationAreaItem)item).parent;
            }
            else if (item.IsEditable() && (typeof(StationItem) == item.GetType()))
            {
                StationDisplay interface2 = new StationDisplay((StationItem)item);
                interface2.ShowDialog();
                edited = item;
            }
            else if (item.IsEditable() && (typeof(AEBufferItem) == item.GetType()))
            {
                ((AEBufferItem)item).Configured = true;
                BufferInterface interface2 = new BufferInterface(((AEBufferItem)item));
                interface2.ShowDialog();
                if (interface2.ToRemove)
                {
                    return null;
                }
                item.Edit();
                edited = ((AEBufferItem)item).parentStation;
            }
            else if (item.IsEditable() && (typeof(TrackSegment) == item.GetType()))
            {
                List<string> destination = new List<string>();
                destination.Add("All");
                ((TrackSegment)item).Configured = true;
                TrackSegmentInterface interface2 = new TrackSegmentInterface(((TrackSegment)item), destination);
                interface2.ShowDialog();
                item.Edit();
                edited = ((TrackSegment)item).parentStation;
            }
            return edited;
        }

        public List<GlobalItem> getActItem()
        {
            if (typeConfig == TypeEditor.ACTIVITY)
            {
                return aeActivity.getActItem();
            }
            return new List<GlobalItem>(0);
        }

        public string getActivityDescr()
        {
            if (typeConfig == TypeEditor.ACTIVITY)
            {
                return aeActivity.activityInfo.ActivityDescr;
            }
            return "";
        }

        public string getActivityName()
        {
            if (typeConfig == TypeEditor.ACTIVITY)
            {
                return aeActivity.activityInfo.ActivityName;
            }
            return "";
        }

        public List<GlobalItem> getORWidget()
        {
            return aeRouteConfig.orRouteConfig.AllItems;
        }

        public List<StationWidgetInfo> getStationWidgets()
        {
            return stationWidgetInfo;
        }

        public List<TagWidgetInfo> getTagWidgets()
        {
            return tagWidgetInfo;
        }

        public void LoadActivity(ActivityInfo activityInfo)
        {
            aeActivity.LoadActivity(activityInfo);
        }

        public void LoadPanels(string name)
        {
            if (typeConfig == TypeEditor.ACTIVITY)
            {
                aeActivity.LoadPanels(AEConfigPanel);
            }
            aeRouteConfig.LoadPanels("RoutePanel", routeData);
        }

        public void LoadRoute()
        {
            tagWidgetInfo = new List<TagWidgetInfo>();
            tagGBList = new List<GroupBox>();
            stationWidgetInfo = new List<StationWidgetInfo>();
            stationGBList = new List<GroupBox>();

            Viewer.actParent.SuspendLayout();
            //System.Drawing.Point current = activityAe.GetPanelPosition();
            foreach (var item in getORWidget())
            {
                if (typeof(TagItem) == item.GetType() || item.typeItem == (int)TypeItem.TAG_ITEM)
                {

                    int cnt = rnd.Next(999999);
                    TagWidgetInfo newTagWidget = new TagWidgetInfo(Viewer, ((TagItem)item), cnt);
                    AddTagWidgetInfo(newTagWidget);


                }
            }

            foreach (var item in getORWidget())
            {
                if (typeof(StationItem) == item.GetType() || item.typeItem == (int)TypeItem.STATION_AREA_ITEM)
                {

                    int cnt = rnd.Next(999999);
                    StationWidgetInfo newStationWidget = new StationWidgetInfo(Viewer, ((StationItem)item), cnt);
                    AddStationInfo(newStationWidget);
                }
            }
            Viewer.actParent.ResumeLayout(false);
            Viewer.actParent.PerformLayout();
        }

        public void RemoveStation(int cntStr)
        {
            if (cntStr < stationWidgetInfo.Count)
            {
                stationWidgetInfo.RemoveAt(cntStr);
                Point stationPanelPosition = aeRouteConfig.GetStationPanelPosition();
                stationWidgetInfo.TrimExcess();
                stationGBList.RemoveAt(cntStr);
                stationGBList.TrimExcess();
                GlobalItem item = aeRouteConfig.orRouteConfig.Index(cntStr);
                aeRouteConfig.orRouteConfig.RemoveItem(item);
                aeRouteConfig.AddStationPanel(stationGBList.ToArray());
            }
        }

        public void RemoveTag(int cntStr)
        {
            if (cntStr < tagWidgetInfo.Count)
            {
                tagWidgetInfo.RemoveAt(cntStr);
                Point tagPanelPosition = aeRouteConfig.GetTagPanelPosition();
                tagWidgetInfo.TrimExcess();
                tagGBList.RemoveAt(cntStr);
                tagGBList.TrimExcess();
                GlobalItem item = aeRouteConfig.orRouteConfig.Index(cntStr);
                aeRouteConfig.orRouteConfig.RemoveItem(item);
                aeRouteConfig.AddTagPanel(tagGBList.ToArray());
            }
        }

        public void Save()
        {
            aeActivity.Save();
            aeRouteConfig.SaveRoute();
        }

        public void setActivityDescr(string info)
        {
            aeActivity.activityInfo.ActivityDescr = info;
        }

        public void setActivityName(string info)
        {
            aeActivity.activityInfo.ActivityName = info;
        }

        public void SetFocus(TypeEditor viewerMode)
        {
            if (viewerMode == TypeEditor.ACTIVITY)
            {
                AEConfigPanel.Visible = true;
                AEConfigPanel.ResumeLayout(false);
            }
            routeData.Visible = true;
            routeData.ResumeLayout(false);
        }

        public void UnsetFocus(TypeEditor viewerMode)
        {
            if (viewerMode == TypeEditor.ACTIVITY)
            {
                AEConfigPanel.SuspendLayout();
                AEConfigPanel.Visible = false;
            }
            routeData.SuspendLayout();
            routeData.Visible = false;
        }

        public GlobalItem UpdateItem(GlobalItem item, MSTSCoord coord, bool controlKey, bool forceConnector)
        {
            PointF closest = new PointF(0f, 0f);
            StationItem parent = null;
            StationAreaItem item3 = null;
            PointF tf2 = new PointF(0f, 0f);
            double snapSize = Viewer.snapSize;
            double num2 = -1.0;
            double positiveInfinity = double.PositiveInfinity;
            PointF tf3 = coord.ConvertToPointF();
            if (item.GetType() == typeof(StationAreaItem))
            {
                item3 = (StationAreaItem)item;
                parent = item3.parent;
                if (parent == null)
                {
                    return null;
                }
            }
            else
            {
                if (typeof(StationItem) == item.GetType())
                {
                    parent = (StationItem)item;
                    foreach (StationAreaItem item4 in parent.stationArea)
                    {
                        StationAreaItem item5 = parent.getNextArea(item4);
                        num2 = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(), new AESegment(item4.Location, item5.Location), out closest);
                        if ((num2 < snapSize) && (num2 < positiveInfinity))
                        {
                            positiveInfinity = num2;
                            item3 = item4;
                        }
                    }
                    parent.Update(coord);
                    return parent;
                }
                if (!item.IsLineSnap())
                {
                    item.Update(coord);
                    return item;
                }
            }
            TrackSegment segment = null;
            int associateNodeIdx = 0;
            foreach (TrackSegment segment2 in aeItems.getSegments())
            {
                segment2.unsetSnap();
                num2 = DrawUtility.FindDistanceToSegment(coord.ConvertToPointF(), segment2, out closest);
                if ((num2 < snapSize) && (num2 < positiveInfinity))
                {
                    positiveInfinity = num2;
                    segment = segment2;
                    segment.setSnap();
                    tf2 = closest;
                    associateNodeIdx = segment.associateNodeIdx;
                }
            }
            if (segment != null)
            {
                segment.setSnap();
            }
            if ((!item.IsLineSnap() || (segment != null)) || controlKey)
            {
                item3.Update(coord);
            }
            return parent;
        }

        public TagWidgetInfo findTagWidget(Predicate<TagWidgetInfo> predicate)
        {
            return tagWidgetInfo.Find(predicate);
        }
    }
}
