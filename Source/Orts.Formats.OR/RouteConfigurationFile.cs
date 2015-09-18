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
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Orts.Formats.OR
{
    /// <summary>
    /// ORRouteConfig is the main class to access the OpenRail specific data for a route.  These data complete the MSTS one in terms of Station
    /// and Station's connectors to track.
    /// The data are saved in json file into the main repository of the route.
    /// </summary>

    public class ORRouteConfig
    {
        [JsonProperty("FileName")]
        public string FileName;
        [JsonProperty("RoutePath")]
        public string RoutePath { get; protected set; }
        [JsonProperty("GlobalItem")]
        public List<GlobalItem> routeItems;    //  Only the items linked to the route Metadata
        [JsonProperty("RouteName")]
        public string RouteName { get; protected set; }
        [JsonProperty("GenAuxAction")]
        public ActionContainer ActionContainer = new ActionContainer();
        [JsonIgnore]
        List<AuxActionRef> GenAuxAction { get { return ActionContainer.GetGenAuxActions(); } set { } }


        [JsonIgnore]
        public List<GlobalItem> AllItems { get; protected set; }       //  All the items, include the activity items, exclude the MSTS Item, not saved
        [JsonIgnore]
        public bool toSave = false;
        [JsonIgnore]
        public AETraveller traveller { get; protected set; }
        [JsonIgnore]
        AETraveller searchTraveller;
        [JsonIgnore]
        public MSTSBase TileBase { get; set; }
        [JsonIgnore]
        public int a;
        [JsonIgnore]
        private System.Object lockThis = new System.Object();

        /// <summary>
        /// The class constructor, but, don't use it.  Prefer to use the static method 'LoadConfig' wich return this object
        /// </summary>
        public ORRouteConfig()
        {
            AllItems = new List<GlobalItem>();
            routeItems = new List<GlobalItem>();
            RouteName = "";
            TileBase = new MSTSBase();
        }

        /// <summary>
        /// SetTileBase is used to initialize the TileBase for the route.  This information is then used to 'reduce' the value of the
        /// MSTS Coordinate wich are too big to be correctly shown in the editor
        /// </summary>
        /// <param name="tileBase"></param>
        public void SetTileBase(MSTSBase tileBase)
        {
            TileBase = tileBase;
        }

        public List<StationItem> GetStationItem()
        {
            List<StationItem> stationList = new List<StationItem>();
            foreach (var item in routeItems)
            {
                if (typeof(StationItem) == item.GetType() || item.typeItem == (int)TypeItem.STATION_ITEM)
                {
                    stationList.Add((StationItem)item);
                }
            }
            return stationList;
        }

        /// <summary>
        /// Use this function to add a new item into the 'AllItems' list.
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(GlobalItem item)
        {
            if (item == null)
                return;
            //if (!(sideItem is PathEventItem) && !(sideItem is SideStartItem))
            if (item.asMetadata)
            {
                if (routeItems.IndexOf(item) < 0)
                    routeItems.Add(item);
            }
            if (AllItems.IndexOf(item) < 0)
                AllItems.Add(item);
            toSave = true;
            if (item.GetType() == typeof(StationItem))
            {
                foreach (StationAreaItem SAItem in ((StationItem)item).stationArea)
                {
                    AllItems.Add(SAItem);
                }
            }
        }

        /// <summary>
        /// Used to remove a connector item from the 'AllItem' list. 
        /// </summary>
        /// <param name="item"></param>
        public void RemoveConnectorItem(GlobalItem item)
        {
            if (item.GetType() == typeof(StationAreaItem))
            {
                AllItems.Remove(item);
                routeItems.Remove(item);
            }
        }

        /// <summary>
        /// Used to remove an item from all list of items: AllItems, RouteItems
        /// </summary>
        /// <param name="item">the item to remove</param>
        public void RemoveItem(GlobalItem item)
        {
            if (item.GetType() == typeof(StationItem))
            {
                foreach (var point in ((StationItem)item).stationArea)
                {
                    RemoveConnectorItem(point);
                }
                ((StationItem)item).stationArea.Clear();
            }
            AllItems.Remove(item);
            routeItems.Remove(item);
            toSave = true;
        }

        public GlobalItem Index(int cnt)
        {
            return AllItems[cnt];
        }

        public int SaveConfig()
        {
            foreach (var item in AllItems)
            {
                item.Unreduce(TileBase);
            }
            return SerializeJSON();
        }

        public void ReduceItems()
        {
            foreach (var item in AllItems)
            {
                item.Reduce(TileBase);
            }

        }

        static public ORRouteConfig LoadConfig(string fileName, string path, TypeEditor interfaceType)
        {
            string completeFileName = Path.Combine(path, fileName);
            ORRouteConfig loaded = DeserializeJSON(completeFileName, interfaceType);
            return loaded;
        }

        public int SerializeJSON()
        {
            lock (lockThis)
            {
                try
                {
                    if (FileName == null || FileName.Length <= 0)
                        return -1;
                    string completeFileName = Path.Combine(RoutePath, FileName);

                    JsonSerializer serializer = new JsonSerializer();
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.TypeNameHandling = TypeNameHandling.All;
                    serializer.Formatting = Formatting.Indented;
                    using (StreamWriter wr = new StreamWriter(completeFileName))
                    {
                        using (JsonWriter writer = new JsonTextWriter(wr))
                        {
                            serializer.Serialize(writer, this);
                        }
                    }
                }
                catch
                {
                    return -1;
                }
            }
            return 0;
        }

        static public ORRouteConfig DeserializeJSON(string fileName, TypeEditor interfaceType)
        {
            ORRouteConfig p;

            fileName += ".cfg.json";
            //try
            //{
                // TODO: This code is BROKEN. It loads and saves file formats with internal type information included, which causes breakages if the types are moved. This is not acceptable for public, shared data.
                //JsonSerializer serializer = new JsonSerializer();
                //using (StreamReader sr = new StreamReader(fileName))
                //{
                //    ORRouteConfig orRouteConfig = JsonConvert.DeserializeObject<ORRouteConfig>((string)sr.ReadToEnd(), new JsonSerializerSettings
                //    {
                //        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                //        TypeNameHandling = TypeNameHandling.Auto
                //    });
                //    p = orRouteConfig;
                    
                //    foreach (var item in p.routeItems)
                //    {
                //        p.AllItems.Add(item);
                //        item.alignEdition(interfaceType, null);
                //        if (item.GetType() == typeof(StationItem))
                //        {
                //            if (((StationItem)item).stationArea.Count > 0)
                //            {
                //                foreach (var item2 in ((StationItem)item).stationArea)
                //                {
                //                    ((StationAreaItem)item2).alignEdition(interfaceType, item);
                //                }
                //                ((StationItem)item).areaCompleted = true;
                //            }
                //        }
                //        else if (item.GetType() == typeof(AEBufferItem))
                //        {
                //        }
                //    }
                //    //orRouteConfig.ReduceItems();
                //}
                //
            //}
            //catch (IOException)
            //{
                p = new ORRouteConfig();
                p.FileName = Path.GetFileName(fileName);
                p.RoutePath = Path.GetDirectoryName(fileName);
                p.RouteName = "";
                p.toSave = true;

            //}
            return p;
        }

        public void SetTraveller(TrackSectionsFile TSectionDat, TrackDatabaseFile TDB)
        {
            TrackNode[] TrackNodes = TDB.TrackDB.TrackNodes;
            traveller = new AETraveller(TSectionDat, TDB);
            foreach (var item in routeItems)
            {
                if (item.GetType() == typeof(StationItem))
                {
                    ((StationItem)item).setTraveller(traveller);
                }
            }

        }

        public void StartSearchPath(TrackPDP startPoint)
        {
            searchTraveller = new AETraveller(this.traveller);
            searchTraveller.place((int)startPoint.TileX, (int)startPoint.TileZ, startPoint.X, startPoint.Z);
        }

        public TrackPDP SearchNextPathNode(TrackPDP endPoint)
        {
            TrItem trItem = null;
            TrackPDP newNode = null;
            trItem = searchTraveller.MoveToNextItem(AllItems, (int)endPoint.TileX, (int)endPoint.TileZ, endPoint.X, endPoint.Z);
            if (trItem != null)
            {
                //newNode = new TrackPDP(trItem);
            }

            return newNode;
        }

        public GlobalItem FindMetadataItem(PointF point, double snapSize, MSTSItems aeItems)
        {
            double positiveInfinity = double.PositiveInfinity;
            double actualDist = double.PositiveInfinity;
            GlobalItem item = null;

            //  First we check only for items except StationItem
            foreach (GlobalItem item2 in AllItems)
            {
                if (item2.GetType() == typeof(StationItem))
                    continue;
                if (!item2.IsEditable() && !item2.IsMovable() && !item2.IsRotable())
                    continue;
                item2.SynchroLocation();
                positiveInfinity = item2.FindItem(point, snapSize, actualDist, aeItems);
                if ((((item != null) && (positiveInfinity <= actualDist)) && ((positiveInfinity == 0.0) || item2.isItSeen())) || (item == null))
                {
                    actualDist = positiveInfinity;
                    item = item2;
                }
            }
            if ((item == null) || (actualDist == double.PositiveInfinity))
            {
                foreach (GlobalItem item2 in AllItems)
                {
                    item2.SynchroLocation();
                    positiveInfinity = item2.FindItem(point, snapSize, actualDist, aeItems);
                    if ((((item != null) && (positiveInfinity <= actualDist)) && ((positiveInfinity == 0.0) || item2.isItSeen())) || (item == null))
                    {
                        actualDist = positiveInfinity;
                        item = item2;
                    }
                }
                if ((item == null) || (actualDist == double.PositiveInfinity))
                {
                    return null;
                }

            }
            item.isSeen = true;
            return item;
        }

        //  Used only in RunActivity
        public StationItem SearchByLocation(WorldLocation location)
        {
            MSTSCoord place = new MSTSCoord(location);
            List<StationItem> listStation = GetStationItem();
            foreach (var item in listStation)
            {
                if (item.IsInStation(place))
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Scan the current orRouteConfig and search for items related to the given node
        /// </summary>
        /// <param name="iNode">The current node index</param>
        /// <param name="orRouteConfig">The Open Rail configuration coming from Editor</param>
        /// <param name="trackNodes">The list of MSTS Track Nodes</param>
        /// <param name="tsectiondat">The list of MSTS Section datas</param>
        public List<TrackCircuitElement> GetORItemForNode(int iNode, TrackNode[] trackNodes, TrackSectionsFile tsectiondat)
        {
            List<TrackCircuitElement> trackCircuitElements = new List<TrackCircuitElement>();
            if (AllItems.Count <= 0)
                return trackCircuitElements;
            foreach (var item in AllItems)
            {
                switch (item.typeItem)
                {
                    case (int)TypeItem.STATION_CONNECTOR:
                        if (item.associateNodeIdx != iNode)
                            continue;
                        TrackNode node = trackNodes[iNode];
                        AETraveller travel = new AETraveller(traveller);
                        travel.place(node);
                        float position = travel.DistanceTo(item);
                        TrackCircuitElement element = (TrackCircuitElement)new TrackCircuitElementConnector(item, position);
                        trackCircuitElements.Add(element);
                        break;
                    default:
                        break;
                }
            }

            return trackCircuitElements;
        }
    }

    /// <summary>
    /// ORConfig is the main class to access the OpenRail generic data for all route.
    /// </summary>

    public class ORConfig
    {
        [JsonProperty("GenAuxAction")]
        List<AuxActionRef> GenAuxAction;

        [JsonIgnore]
        string FileName;
        [JsonIgnore]
        bool CanSaveConfig = false;

        public ORConfig()
        {
            GenAuxAction = new List<AuxActionRef>();
        }

        public void UpdateGenAction(bool info, AuxActionRef.AUX_ACTION typeAction)
        {
            switch (typeAction)
            {
                case AuxActionRef.AUX_ACTION.SOUND_HORN:
                    break;
                default:
                    break;
            }
        }

        public bool SaveConfig()
        {
            if (!CanSaveConfig)
                return false;
            return SerializeJSON();
        }

        static public ORConfig LoadConfig(string dataFolder, ORConfig mainConfig = null)
        {
            string currentProgFolder = dataFolder;
            currentProgFolder = Path.GetDirectoryName(currentProgFolder);
            string completeFileName = Path.Combine(currentProgFolder, "Open Rails");
            if (!Directory.Exists(completeFileName)) Directory.CreateDirectory(completeFileName);
            completeFileName = Path.Combine (completeFileName, "ORConfig.json");
            ORConfig loaded = DeserializeJSON(completeFileName);
            return loaded;
        }

        public bool SerializeJSON()
        {
            if (FileName == null || FileName.Length <= 0)
                return false;

            JsonSerializer serializer = new JsonSerializer();
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            serializer.TypeNameHandling = TypeNameHandling.All;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter wr = new StreamWriter(FileName))
            {
                using (JsonWriter writer = new JsonTextWriter(wr))
                {
                    serializer.Serialize(writer, this);
                }
            }
            return true;
        }

        static public ORConfig DeserializeJSON(string fileName)
        {
            ORConfig p = null;

            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(fileName))
                {
                    ORConfig orConfig = JsonConvert.DeserializeObject<ORConfig>((string)sr.ReadToEnd(), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                    p = orConfig;
                    p.FileName = fileName;
                    p.CanSaveConfig = false;
                }
            }
            catch
            {
                p = new ORConfig();
                p.FileName = fileName;
                p.CanSaveConfig = false;
            }
            return p;
        }
    }
}
