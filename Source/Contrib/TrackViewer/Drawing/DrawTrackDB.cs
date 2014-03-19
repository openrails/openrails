// COPYRIGHT 2014 by the Open Rails project.
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
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MSTS.Formats;
using ORTS.Common;
using ORTS.TrackViewer.Properties;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This is a big class where the drawing of everything in the track data base is done. 
    /// This means tracks themselves (meaning so-called vector nodes that contain a number of sections,
    /// each of which is drawn separately), junctions and endnodes (drawn using textures), track items (platforms,
    /// sidings, signals, hazards, ...), and the same for roads and road-items.
    /// The methods for these are DrawTracks, DrawRoads, DrawJunctionAndEndNodes, DrawTrackItems, DrawRoadTrackItems
    /// For all things drawn it is also tracked which of these things is closest to the (current) mouse location.
    /// Those particular things are then re-drawn in highlight colors. These things are also available for other
    /// uses, like a statusbar and using it for path editor. The method to call is DrawHighlights
    /// The drawing itself is done by calls to routines in drawarea, that translates world-coordinates to screen coordinates 
    /// and then calls basic drawing routines.
    /// 
    /// The constructor will read the needed files (using other classes to do this).
    /// 
    /// There are also a number of methods to find specific items or tracks given their index, such that the user can search for them.
    /// 
    /// At last there are a number of utility methods like GetLength, UIDlocation.
    /// </summary>
    public class DrawTrackDB
    {
        /// <summary>Track Section Data, public such that other classes have access as well</summary>
        public TSectionDatFile tsectionDat { get; private set; }
        /// <summary>Track database, public such that other classes have access as well</summary>
        public TrackDB trackDB { get; private set; }

        /// <summary>Road track database</summary>
        private RoadTrackDB roadTrackDB;
        /// <summary>Direction-angle of track (needed for signal), indexed by TrItemID </summary>
        private Dictionary<uint, float> signalAngles = new Dictionary<uint, float>();
        /// <summary>Direction-angle of track indexed by tracknode index (of the endnode)</summary>
        private Dictionary<uint, float> endnodeAngles = new Dictionary<uint, float>();
        /// <summary>(approximate) world location of the sidings indexed by siding name</summary>
        public Dictionary<string, WorldLocation> sidingLocations;
        /// <summary>(approximate) world location og the platforms indexed by platform name</summary>
        public Dictionary<string, WorldLocation> platformLocations;


        /// <summary>Name of the route</summary>
        public string RouteName { get; private set; }
        /// <summary>Full filename of the route</summary>
        private string roadTrackFileName;

        // Maximal and minimal tile numbers from the track database
        /// <summary>Maximum of the TileX index found in the track database</summary>
        public int MaxTileX { get; private set; }
        /// <summary>Minimum of the TileX index found in the track database</summary>
        public int MinTileX { get; private set; }
        /// <summary>Maximum of the TileZ index found in the track database</summary>
        public int MaxTileZ { get; private set; }
        /// <summary>Minimum of the TileZ index found in the track database</summary>
        public int MinTileZ { get; private set; }

        /// <summary>Rail (so not road) track closest to the mouse</summary>
        private CloseToMouseTrack closestRailTrack;
        /// <summary>Road track closest to the mouse</summary>
        public CloseToMouseTrack closestRoadTrack;
        /// <summary>Either Road or Rail track (but must be drawn) that is closest to the mouse</summary>
        public CloseToMouseTrack closestTrack;
        /// <summary>The drawn junction or end node that is closest to the mouse</summary>
        public CloseToMouseJunctionOrEnd closestJunctionOrEnd = new CloseToMouseJunctionOrEnd();
        /// <summary>The drawn track item (either road or rail) that is closest to the mouse</summary>
        public CloseToMouseItem closestTrItem = new CloseToMouseItem();

        /// <summary>Normally highlights are based on mouse location. When searching this is overridden</summary>
        private static Boolean IsHighlightOverridden = false;
        /// <summary>Normally highlights are based on mouse location. When searching this is overridden</summary>
        static Boolean IsHighlightOverriddenTrItem = false;

        /// <summary>If the itemNames have not been created, this is false</summary>
        static bool IsItemNameCreated = false;
        /// <summary>Name of the item type indexed by the type itself</summary>
        public static Dictionary<TrItem.trItemType, string> itemName;

        // various fields to optimize drawing efficiency
        int tileXIndexStart;
        int tileXIndexStop;
        int tileZIndexStart;
        int tileZIndexStop;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="RoutePath">Path to the route directory</param>
        /// <param name="messageDelegate">The delegate that will deal with the message we want to send to the user</param>
        public DrawTrackDB(string RoutePath, TrackViewer.messageDelegate messageDelegate)
        {
            if (!IsItemNameCreated) CreateItemNames();

            messageDelegate(TrackViewer.catalog.GetString("Loading trackfile .trk ..."));
            TRKFile TRK = new TRKFile(MSTS.MSTSPath.GetTRKFileName(RoutePath));
            RouteName = TRK.Tr_RouteFile.Name;

            messageDelegate(TrackViewer.catalog.GetString("Loading track database .tdb ..."));
            TDBFile TDB = new TDBFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");
            this.trackDB = TDB.TrackDB;
            FindExtremeTiles();

            messageDelegate(TrackViewer.catalog.GetString("Loading tsection.dat ..."));
            string BasePath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));
            if (Directory.Exists(RoutePath + @"\GLOBAL") && File.Exists(RoutePath + @"\GLOBAL\TSECTION.DAT"))
                tsectionDat = new TSectionDatFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            else
                tsectionDat = new TSectionDatFile(BasePath + @"\GLOBAL\TSECTION.DAT");
            if (File.Exists(RoutePath + @"\TSECTION.DAT"))
                tsectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");

            roadTrackFileName = RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".rdb";

            messageDelegate(TrackViewer.catalog.GetString("Finding the angles to draw signals ..."));
            FindSignalOrientations();
            FindEndnodeOrientations();
            FindSidingsAndPlatforms();
            FillAvailableIndexes();

            closestRailTrack = new CloseToMouseTrack(tsectionDat);
            closestRoadTrack = new CloseToMouseTrack(tsectionDat);
           
        }

        /// <summary>
        /// Load the road track database. The filename has been stored before
        /// </summary>
        void LoadRoadTrackDB ()
        {
            // since this is being called from within a Game.Draw method, we cannot use messageDelagate (which also is a Game.Draw method).
            try
            {
                RDBFile RDB = new RDBFile(roadTrackFileName);
                roadTrackDB = RDB.RoadTrackDB;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Determines the minimal and maximale tileX/Z from the database.
        /// </summary>
        private void FindExtremeTiles()
        {
            MinTileX = +1000000;
            MinTileZ = +1000000;
            MaxTileX = -1000000;
            MaxTileZ = -1000000;
            for (var tni = 0; tni < trackDB.TrackNodes.Length; tni++)
            {
                TrackNode tn = trackDB.TrackNodes[tni];
                if (tn == null) continue;

                if (tn.TrVectorNode != null)
                {
                    for (int tvsi = 0; tvsi < tn.TrVectorNode.TrVectorSections.Length; tvsi++)
                    {
                        TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                        if (tvs.TileX < MinTileX) { MinTileX = tvs.TileX; };
                        if (tvs.TileZ < MinTileZ) { MinTileZ = tvs.TileZ; };
                        if (tvs.TileX > MaxTileX) { MaxTileX = tvs.TileX; };
                        if (tvs.TileZ > MaxTileZ) { MaxTileZ = tvs.TileZ; };
                    }
                }
            }
        }

        /// <summary>
        /// Find, for each signal, the orientation/angle we need to draw it
        /// </summary>
        void FindSignalOrientations()
        {
            foreach (TrackNode tn in trackDB.TrackNodes)
            {
                if (tn == null) continue;
                TrVectorNode tvn= tn.TrVectorNode;
                if (tvn == null) continue;
                if (tvn.TrItemRefs == null) continue;

                foreach (int trackItemIndex in tvn.TrItemRefs)
                {
                    TrItem trackItem = trackDB.TrItemTable[trackItemIndex];
                    if (trackItem is SignalItem)
                    {
                        float angle = 0;
                        try
                        {
                            SignalItem signalItem = trackItem as SignalItem;
                            Traveller.TravellerDirection direction = signalItem.Direction == 0 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward;
                            Traveller signalTraveller = new Traveller(tsectionDat, trackDB.TrackNodes, tn, 
                                trackItem.TileX, trackItem.TileZ, trackItem.X, trackItem.Z, direction);
                            angle = signalTraveller.RotY;
                        }
                        catch { }
                        signalAngles[trackItem.TrItemId] = angle;
                    }
                }
                
            }
        }

        /// <summary>
        /// For each endnode, find its orientattion. So we can draw a line in the correct direction.
        /// </summary>
        void FindEndnodeOrientations()
        {
            for (var tni = 0; tni < trackDB.TrackNodes.Length; tni++)
            {
                TrackNode tn = trackDB.TrackNodes[tni];
                if (tn == null) continue;
                endnodeAngles[tn.Index] = 0;//default value in case we cannot find a better one

                if (tn.TrEndNode)
                {
                    int connectedVectorNodeIndex = tn.TrPins[0].Link;
                    TrackNode connectedVectorNode = trackDB.TrackNodes[connectedVectorNodeIndex];
                    if (connectedVectorNode == null) continue;
                    if (connectedVectorNode.TrVectorNode == null) continue;

                    if (connectedVectorNode.TrPins[0].Link == tni)
                    {
                        //find angle at beginning of vector node
                        TrVectorSection tvs = connectedVectorNode.TrVectorNode.TrVectorSections[0];
                        endnodeAngles[tn.Index] = tvs.AY;
                    }
                    else
                    {
                        //find angle at end of vector node
                        TrVectorSection tvs = connectedVectorNode.TrVectorNode.TrVectorSections.Last();
                        endnodeAngles[tn.Index] = tvs.AY;
                        try
                        { // try to get even better in case the last section is curved
                            TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);
                            if (trackSection.SectionCurve != null)
                            {
                                endnodeAngles[tn.Index] += MathHelper.ToRadians(trackSection.SectionCurve.Angle);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Generate a list of locations for all platforms and for all sidings, so people can go to these locations from menu
        /// </summary>
        void FindSidingsAndPlatforms()
        {
            sidingLocations = new Dictionary<string, WorldLocation>();
            platformLocations = new Dictionary<string, WorldLocation>();

            foreach (TrItem trackItem in trackDB.TrItemTable)
            {
                switch (trackItem.ItemType)
                {
                    case TrItem.trItemType.trSIDING:
                        sidingLocations[trackItem.ItemName] = itemLocation(trackItem);
                        break;
                    case TrItem.trItemType.trPLATFORM:
                        platformLocations[trackItem.ItemName] = itemLocation(trackItem);
                        break;
                }
                
            }
        }

        static void CreateItemNames()
        {
            itemName = new Dictionary<TrItem.trItemType,string>();
            itemName[TrItem.trItemType.trCarSpawner] = "carspawner";
            itemName[TrItem.trItemType.trCROSSOVER] = "crossover";
            itemName[TrItem.trItemType.trEMPTY] = "empty";
            itemName[TrItem.trItemType.trHAZZARD] = "hazard";
            itemName[TrItem.trItemType.trPICKUP] = "pickup";
            itemName[TrItem.trItemType.trPLATFORM] = "platform";
            itemName[TrItem.trItemType.trSIDING] = "siding";
            itemName[TrItem.trItemType.trSIGNAL] = "signal";
            itemName[TrItem.trItemType.trSOUNDREGION] = "soundregion";
            itemName[TrItem.trItemType.trSPEEDPOST] = "speedpost";
            itemName[TrItem.trItemType.trXING] = "crossing";
            IsItemNameCreated = true;
        }

        #region Cache available tracknodes, items, etc per tile
        /// <summary>
        /// In preparation of actual drawing we first have to know which tiles are visible.
        /// And then we translate the visible tiles to array/list start and stop indexes to be used
        /// </summary>
        /// <param name="drawArea">The area upon which we draw, which determines the visible tiles</param>
        void PrepareDrawing(DrawArea drawArea)
        {
            // determine the min and max values of the tiles that we actually need to draw
            // in some cases (e.g. during initialization) the drawing area itself is really outside the track database,
            // so we have to account for that.
            int actualTileXLeft  = Math.Max(Math.Min(drawArea.LocationUpperLeft.TileX , MaxTileX), MinTileX);
            int actualTileXRight = Math.Min(Math.Max(drawArea.LocationLowerRight.TileX, MinTileX), MaxTileX);
            int actualTileZBot   = Math.Max(Math.Min(drawArea.LocationLowerRight.TileZ, MaxTileZ), MinTileZ);
            int actualTileZTop   = Math.Min(Math.Max(drawArea.LocationUpperLeft.TileZ , MinTileZ), MaxTileZ);

            SetTileIndexes(actualTileXLeft, actualTileXRight, actualTileZBot, actualTileZTop);
        }

        /// <summary>
        /// Translate the min and max values of the tileX and tileY into indexes to be used in 'availability' lists
        /// </summary>
        private void SetTileIndexes(int actualTileXLeft, int actualTileXRight, int actualTileZBot, int actualTileZTop)
        {
            tileXIndexStart = actualTileXLeft - MinTileX;
            tileXIndexStop = actualTileXRight - MinTileX;
            tileZIndexStart = actualTileZBot - MinTileZ;
            tileZIndexStop = actualTileZTop - MinTileZ;
        }

        /// <summary>
        /// For each of the various types of tracknodes we list the ones per tile.
        /// </summary>
        List<TrackNode>[,] availableRailVectorNodeIndexes;
        List<TrackNode>[,] availableRoadVectorNodeIndexes;
        List<TrackNode>[,] availablePointNodeIndexes;
        List<TrItem>[,] availableRailItemIndexes;
        List<TrItem>[,] availableRoadItemIndexes;

        /// <summary>
        /// Run over the track databases, find the locations of nodes and items, and add the nodes and items to the correct
        /// 'available' list, indexed by tile.
        /// </summary>
        void FillAvailableIndexes()
        {
            SetTileIndexes(MinTileX, MaxTileX, MinTileZ, MaxTileZ);
            availableRailVectorNodeIndexes = new List<TrackNode>[tileXIndexStop + 1, tileZIndexStop + 1];
            availableRoadVectorNodeIndexes = new List<TrackNode>[tileXIndexStop + 1, tileZIndexStop + 1];
            availablePointNodeIndexes      = new List<TrackNode>[tileXIndexStop + 1, tileZIndexStop + 1];
            availableRailItemIndexes       = new List<TrItem>   [tileXIndexStop + 1, tileZIndexStop + 1];
            availableRoadItemIndexes       = new List<TrItem>   [tileXIndexStop + 1, tileZIndexStop + 1];
            InitIndexedLists(availableRailVectorNodeIndexes);
            InitIndexedLists(availableRoadVectorNodeIndexes);
            InitIndexedLists(availablePointNodeIndexes);
            InitIndexedLists(availableRailItemIndexes);
            InitIndexedLists(availableRoadItemIndexes);

            // find rail track tracknodes
            for (uint tni = 0; tni < trackDB.TrackNodes.Length; tni++)
            {
                TrackNode tn = trackDB.TrackNodes[tni];
                if (tn == null) continue;

                if (tn.TrVectorNode == null)
                {   // so junction or endnode
                    AddLocationToAvailableList(UiDLocation(tn.UiD), availablePointNodeIndexes, tn);
                }
                else
                {   // vector nodes
                    for (int tvsi = 0; tvsi < tn.TrVectorNode.TrVectorSections.Length; tvsi++)
                    {
                        TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                        if (tvs == null) continue;
                        List<WorldLocation> locationList = FindLocationList(tni, tvsi, true);
                        foreach (WorldLocation location in locationList) {
                            AddLocationToAvailableList(location, availableRailVectorNodeIndexes, tn);
                        }
                    }
                }
            }

            //find road tracknodes
            if (roadTrackDB == null) LoadRoadTrackDB();
            if (roadTrackDB != null)
            {
                for (uint tni = 0; tni < roadTrackDB.TrackNodes.Length; tni++)
                {
                    TrackNode tn = roadTrackDB.TrackNodes[tni];
                    if (tn == null) continue;

                    if (tn.TrVectorNode != null)
                    {
                        for (int tvsi = 0; tvsi < tn.TrVectorNode.TrVectorSections.Length; tvsi++)
                        {
                            TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                            if (tvs == null) continue;
                            List<WorldLocation> locationList = FindLocationList(tni, tvsi, false);
                            foreach (WorldLocation location in locationList)
                            {
                                AddLocationToAvailableList(location, availableRoadVectorNodeIndexes, tn);
                            }
                        }
                    }
                }
            }

            // find rail track items
            foreach (TrItem trackItem in trackDB.TrItemTable)
            {
                AddLocationToAvailableList(itemLocation(trackItem), availableRailItemIndexes, trackItem);
            }

            // find road track items
            if (roadTrackDB != null && roadTrackDB.TrItemTable != null)
            {
                foreach (TrItem trackItem in roadTrackDB.TrItemTable)
                {
                    AddLocationToAvailableList(itemLocation(trackItem), availableRoadItemIndexes, trackItem);
                }
            }
 
            // remove double entries
            MakeUniqueLists(ref availableRailVectorNodeIndexes);
            MakeUniqueLists(ref availableRoadVectorNodeIndexes);
            MakeUniqueLists(ref availablePointNodeIndexes);
            MakeUniqueLists(ref availableRailItemIndexes);
            MakeUniqueLists(ref availableRoadItemIndexes);
        }

        /// <summary>
        /// From the location find the tile and then the corresponding indexes for our arrays/lists
        /// And then add the given item to the given list at the correct indexes
        /// </summary>
        /// <typeparam name="T">Type of the item we want to add to the list.</typeparam>
        /// <param name="location">Worldlocation of the item, that gives us the tile indexes</param>
        /// <param name="ArrayOfListsToAddTo">To which list we have to add the item</param>
        /// <param name="item">The item we want to add to the list, at the correct index</param>
        void AddLocationToAvailableList<T>(WorldLocation location, List<T>[,] ArrayOfListsToAddTo, T item)
        {
            //possibly the location is out of the allowed region (e.g. because possibly undefined).
            if (location.TileX < MinTileX || location.TileX > MaxTileX || location.TileZ < MinTileZ || location.TileZ > MaxTileZ) return;
            int TileXIndex = location.TileX - MinTileX;
            int TileZIndex = location.TileZ - MinTileZ;
            ArrayOfListsToAddTo[TileXIndex, TileZIndex].Add(item);
        }

        /// <summary>
        /// basically just make sure all elements in the two dimensional array have an empty list to start with.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemlist"></param>
        void InitIndexedLists<T>(List<T>[,] itemlist)
        {
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    itemlist[xindex, zindex] = new List<T>();
                }
            }
        }

        /// <summary>
        /// For each list in the given 2D array, make sure the list contains only unique elements
        /// </summary>
        /// <typeparam name="T">Type of object that is in the list (not actually used)</typeparam>
        /// <param name="arrayOfLists">2D array containing non-null lists</param>
        void MakeUniqueLists<T>(ref List<T>[,] arrayOfLists)
        {
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    arrayOfLists[xindex, zindex] = arrayOfLists[xindex, zindex].Distinct().ToList();
                }
            }    
        }

        /// <summary>
        /// For a vector section, generate a list of world-locations that are used to determine whether or not the
        /// vector section will be drawn when a tile is visible
        /// </summary>
        /// <param name="tracknodeindex">Index of the tracknode</param>
        /// <param name="TrackVectorSectionIndex">Index of the vector section in the tracknode</param>
        /// <param name="useRailTracks">Must we use rail or road tracks</param>
        /// <returns>A list of world locations on the vector section</returns>
        List<WorldLocation> FindLocationList(uint tracknodeindex, int TrackVectorSectionIndex, bool useRailTracks)
        {
            List<WorldLocation> resultList = new List<WorldLocation>();
            TrackSection trackSection = tsectionDat.TrackSections.Get((uint) TrackVectorSectionIndex);
            if (trackSection == null) return resultList;
            float trackSectionLength = DrawTrackDB.GetLength(trackSection);
            int imax = 4; // we add 4+1 points along the track, including begin and end
            for (int i = 0; i <= imax; i++)
            {
                WorldLocation newLocation = FindLocation(tracknodeindex, TrackVectorSectionIndex, i * trackSectionLength / imax, useRailTracks);
                if (newLocation != null)
                {
                    resultList.Add(newLocation);
                }
            }
            return resultList;
        }

       #endregion

        /// <summary>
        /// Draw the tracks from the track database 
        /// </summary>
        /// <param name="drawArea">The drawing area to draw upon</param>
        public void DrawTracks(DrawArea drawArea)
        {
            PrepareDrawing(drawArea);
            closestRailTrack.Reset();

            bool[] hasBeenDrawn = new bool[trackDB.TrackNodes.Length];
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    foreach (TrackNode tn in availableRailVectorNodeIndexes[xindex, zindex])
                    {
                        if (hasBeenDrawn[tn.Index]) continue;
                        DrawVectorNode(drawArea, tn, DrawColors.colorsNormal, closestRailTrack);
                        hasBeenDrawn[tn.Index] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Draw all the roads (if settings are right), in the same way as drawing the tracks
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        public void DrawRoads(DrawArea drawArea)
        {
            if (!Properties.Settings.Default.drawRoads) return;
            //if (roadTrackDB == null) LoadRoadTrackDB();
            PrepareDrawing(drawArea);

            closestRoadTrack.Reset();
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    foreach (TrackNode tn in availableRailVectorNodeIndexes[xindex, zindex])
                    {
                        DrawVectorNode(drawArea, tn, DrawColors.colorsRoads, closestRoadTrack);
                    }
                }
            }
        }


        /// <summary>
        /// Draw the various highlights (tracks/roads and items/junctions/endnodes, based on what is closest to the mouse)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="doAll">highlight everything possible or just train tracks</param>
        public void DrawHighlights(DrawArea drawArea, bool doAll)
        {
            CheckForHighlightOverride();
            if (doAll)
            {
                if (Properties.Settings.Default.drawRoads && closestRoadTrack.IsCloserThan(closestRailTrack))
                {   // high light the closest road track
                    closestTrack = closestRoadTrack;
                    DrawHighlightTracks(drawArea, closestRoadTrack, DrawColors.colorsRoadsHighlight, DrawColors.colorsRoadsHotlight);
                }
                else
                {   //highlight the closest train track
                    closestTrack = closestRailTrack;
                    DrawHighlightTracks(drawArea, closestRailTrack, DrawColors.colorsHighlight, DrawColors.colorsHotlight);
                }

                if (closestTrItem.trItem != null && closestTrItem.IsCloserThan(closestJunctionOrEnd))
                {
                    // Highlight the closest track item
                    drawTrackItem(drawArea, closestTrItem.trItem, DrawColors.colorsHighlight);
                }
                else if (closestJunctionOrEnd.junctionOrEndNode != null)
                {   // Highlight the closest junction
                    if (closestJunctionOrEnd.type == "junction")
                    {
                        DrawJunctionNode(drawArea, closestJunctionOrEnd.junctionOrEndNode, DrawColors.colorsHighlight);
                    }
                    else
                    {
                        DrawEndNode(drawArea, closestJunctionOrEnd.junctionOrEndNode, DrawColors.colorsHighlight);
                    }
                }
            }
            else
            { // basically for inset only
                DrawHighlightTracks(drawArea, closestRailTrack, DrawColors.colorsHighlight, DrawColors.colorsHotlight);
            }
        }

        /// <summary>
        /// Highlight tracks (either from train or from road track)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="closeToMouseTrack">The train/road track that is closest to the mouse</param>
        /// <param name="highColors">Colorscheme for highlights</param>
        /// <param name="hotColors">Colorscheme for hotlights</param>
        private void DrawHighlightTracks(DrawArea drawArea, CloseToMouseTrack closeToMouseTrack, ColorScheme highColors, ColorScheme hotColors)
        {
            DrawVectorNode(drawArea, closeToMouseTrack.TrackNode, highColors, null);
            if (Properties.Settings.Default.statusShowVectorSections)
            {
                DrawTrackSection(drawArea, closeToMouseTrack.TrackNode, closeToMouseTrack.VectorSection, hotColors, null, -1);
            }
        }


        /// <summary>
        /// Draw the track of a MSTS vectorNode (from track database)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="tn">The tracknode from track database (assumed to be a vector node)</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="closeToMouseTrack">The object to track which vector node is closest to the mouse</param>
        void DrawVectorNode(DrawArea drawArea, TrackNode tn, ColorScheme colors, CloseToMouseTrack closeToMouseTrack)
        {
            if (tn == null) return;
            for (int tvsi = 0; tvsi < tn.TrVectorNode.TrVectorSections.Length; tvsi++)
            {
                TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                DrawTrackSection(drawArea, tn, tvs, colors, closeToMouseTrack, tvsi);
            }
        }

        /// <summary>
        /// Draw a specific vectorSection of a vectorNode
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="tn">The tracknode from track database (assumed to be a vector node)</param>
        /// <param name="tvs">The vectorSection itself that needs to be drawn</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="closeToMouseTrack">The object to track which vector node is closest to the mouse</param>
        /// <param name="tvsi">The index of the trackvector section, needed only for closeToMouseTrack</param>
        /// <remarks>Note that his is very similar to DrawTrackSection in class DrawPath, but this one always
        /// draws the whole section and it checks the distance to the mouse</remarks>
        private void DrawTrackSection(DrawArea drawArea, TrackNode tn, TrVectorSection tvs, ColorScheme colors, CloseToMouseTrack closeToMouseTrack, int tvsi)
        {
            if (tvs == null) return;
            TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);
            if (trackSection == null) return;

            WorldLocation thisLocation = new WorldLocation(tvs.TileX, tvs.TileZ, tvs.X, 0, tvs.Z);
            if (closeToMouseTrack != null)
            {
                closeToMouseTrack.CheckMouseDistance(thisLocation, drawArea.mouseLocation, tn, tvs, tvsi);
            }

            if (trackSection.SectionCurve != null)
            {
                drawArea.DrawArc(trackSection.SectionSize.Width, colors["trackCurved"], thisLocation,
                    trackSection.SectionCurve.Radius, tvs.AY, trackSection.SectionCurve.Angle, 0);
            }
            else
            {
                drawArea.DrawLine(trackSection.SectionSize.Width, colors["trackStraight"], thisLocation,
                    trackSection.SectionSize.Length, tvs.AY, 0);
            }
        }

        /// <summary>
        /// Draw all the junction and endNodes
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        public void DrawJunctionAndEndNodes(DrawArea drawArea)
        {
            closestJunctionOrEnd.Reset();
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    foreach (TrackNode tn in availablePointNodeIndexes[xindex, zindex])
                    {
                        if (tn.TrJunctionNode != null && Properties.Settings.Default.showJunctionNodes)
                        {
                            DrawJunctionNode(drawArea, tn, DrawColors.colorsNormal);
                        }

                        if (tn.TrEndNode && Properties.Settings.Default.showEndNodes)
                        {
                            DrawEndNode(drawArea, tn, DrawColors.colorsNormal);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw a specific junction node.
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="tn">The trackNode (assumed to be a activeNodeAsJunction)</param>
        /// <param name="colors">The colorscheme to use for drawing the activeNodeAsJunction</param>
        private void DrawJunctionNode(DrawArea drawArea, TrackNode tn, ColorScheme colors)
        {
            WorldLocation thisLocation = UiDLocation(tn.UiD);
            closestJunctionOrEnd.CheckMouseDistance(thisLocation, drawArea.mouseLocation, tn, "junction");
            drawArea.DrawSimpleTexture(thisLocation, "disc", 1f, 0, colors["junction"]);
        }

        /// <summary>
        /// Draw a specific end node.
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="tn">The trackNode (assumed to be a activeNodeAsJunction)</param>
        /// <param name="colors">The colorscheme to use for drawing the activeNodeAsJunction</param>
        private void DrawEndNode(DrawArea drawArea, TrackNode tn, ColorScheme colors)
        {
            WorldLocation thisLocation = UiDLocation(tn.UiD);
            closestJunctionOrEnd.CheckMouseDistance(thisLocation, drawArea.mouseLocation, tn, "endnode");
            float angle = endnodeAngles[tn.Index];
            drawArea.DrawLine(3f, colors["endnode"], thisLocation, 2f, angle, 0);
        }

        /// <summary>
        /// Draw the various track items like signals, crossings, etc
        /// </summary>
        /// <param name="drawArea">Area to draw the items on</param>
        public void DrawTrackItems(DrawArea drawArea)
        {
            closestTrItem.Reset();
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    foreach (TrItem trackItem in availableRailItemIndexes[xindex, zindex])
                    {
                        drawTrackItem(drawArea, trackItem, DrawColors.colorsNormal);
                    }
                }
            }
       }

        /// <summary>
        /// Draw the various road track items, mainly car spawners but also level crossings (again).
        /// </summary>
        /// <param name="drawArea">Area to draw the items on</param>
        public void DrawRoadTrackItems(DrawArea drawArea)
        {
            // we only want the carspawners here
            if (!Properties.Settings.Default.showCarSpawners && !Properties.Settings.Default.showRoadCrossings) return;
            
            for (int xindex = tileXIndexStart; xindex <= tileXIndexStop; xindex++)
            {
                for (int zindex = tileZIndexStart; zindex <= tileZIndexStop; zindex++)
                {
                    foreach (TrItem trackItem in availableRoadItemIndexes[xindex, zindex])
                    {
                        drawTrackItem(drawArea, trackItem, DrawColors.colorsNormal);
                    }
                }
            }
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="trackItem">The trackItem which you want to draw</param>
        /// <param name="colors">The colorscheme to use</param>
        private void drawTrackItem(DrawArea drawArea, TrItem trackItem, ColorScheme colors)
        {
            switch (trackItem.ItemType)
            {
                case TrItem.trItemType.trEMPTY:
                    break;
                case TrItem.trItemType.trCROSSOVER:
                    break;
                case TrItem.trItemType.trSIGNAL:
                    drawSignal(drawArea, trackItem as SignalItem, colors);
                    break;
                case TrItem.trItemType.trSPEEDPOST:
                    drawSpeedPost(drawArea, trackItem as SpeedPostItem, colors);
                    break;
                case TrItem.trItemType.trPLATFORM:
                    drawPlatform(drawArea, trackItem as PlatformItem, colors);
                    break;
                case TrItem.trItemType.trSOUNDREGION:
                    drawSoundRegion(drawArea, trackItem as SoundRegionItem, colors);
                    break;
                case TrItem.trItemType.trXING:
                    if (trackItem is LevelCrItem)     drawCrossing    (drawArea, trackItem as LevelCrItem, colors);
                    if (trackItem is RoadLevelCrItem) drawRoadCrossing(drawArea, trackItem as RoadLevelCrItem, colors);
                    break;
                case TrItem.trItemType.trSIDING:
                    drawSiding(drawArea, trackItem as SidingItem, colors);
                    break;
                case TrItem.trItemType.trHAZZARD:
                    drawHazard(drawArea, trackItem as HazzardItem, colors);
                    break;
                case TrItem.trItemType.trPICKUP:
                    drawPickup(drawArea, trackItem as PickupItem, colors);
                    break;
                case TrItem.trItemType.trCarSpawner:
                    //happens only when called from road data base
                    drawCarSpawner(drawArea, trackItem as CarSpawnerItem, colors);
                    break;
                default:
                    break;
            }

        }

        private void drawSiding(DrawArea drawArea, SidingItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showSidingMarkers || IsHighlightOverriddenTrItem)
            {
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
                drawArea.DrawSimpleTexture(thisLocation, "disc", 3f, 0, colors["siding"]);
            }
            if (Properties.Settings.Default.showSidingNames || IsHighlightOverriddenTrItem)
            {
                drawArea.DrawString(thisLocation, item.ItemName);
            }
        }

        private void drawPlatform(DrawArea drawArea, PlatformItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showPlatformMarkers || IsHighlightOverriddenTrItem)
            {
                float angle = 0;
                float size = 9f; // in meters
                int minPixelSize = 7;
                drawArea.DrawTexture(thisLocation, "platform" + colors.nameExtension, angle, size, minPixelSize);

                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
            if (Properties.Settings.Default.showPlatformNames || IsHighlightOverriddenTrItem)
            {
                drawArea.DrawString(thisLocation, item.ItemName);
            }
        }

        private void drawCrossing(DrawArea drawArea, LevelCrItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showCrossings || IsHighlightOverriddenTrItem)
            {
                drawArea.DrawSimpleTexture(thisLocation, "disc", 3f, 0, colors["crossing"]);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawRoadCrossing(DrawArea drawArea, RoadLevelCrItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showRoadCrossings || IsHighlightOverriddenTrItem)
            {
                drawArea.DrawSimpleTexture(thisLocation, "disc", 2f, 0, colors["road crossing"]);// smaller than normal crossings
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawSpeedPost(DrawArea drawArea, SpeedPostItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (item.IsLimit && (Properties.Settings.Default.showSpeedLimits || IsHighlightOverriddenTrItem))
            {
                drawArea.DrawSimpleTexture(thisLocation, "disc", 3f, 0, colors["speedpost"]);
                string speed = item.SpeedInd.ToString();
                drawArea.DrawString(thisLocation, speed);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
            if (item.IsMilePost && (Properties.Settings.Default.showMileposts || IsHighlightOverriddenTrItem))
            {
                drawArea.DrawSimpleTexture(thisLocation, "disc", 3f, 0, colors["speedpost"]);
                string distance = item.SpeedInd.ToString();
                drawArea.DrawString(thisLocation, distance);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawSignal(DrawArea drawArea, SignalItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showSignals || IsHighlightOverriddenTrItem)
            {
                float angle = signalAngles[item.TrItemId];
                float size = 9f; // in meters
                int minPixelSize = 9;
                drawArea.DrawTexture(thisLocation, "signal" + colors.nameExtension, angle, size, minPixelSize);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawHazard(DrawArea drawArea, HazzardItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showHazards | IsHighlightOverriddenTrItem)
            {
                float angle = 0;
                float size = 9f; // in meters
                int minPixelSize = 7;
                drawArea.DrawTexture(thisLocation, "hazard" + colors.nameExtension, angle, size, minPixelSize);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }


        private void drawCarSpawner(DrawArea drawArea, CarSpawnerItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showCarSpawners || IsHighlightOverriddenTrItem)
            {
                float angle = 0;
                float size = 9f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(thisLocation, "carspawner" + colors.nameExtension, angle, size, minPixelSize);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawSoundRegion(DrawArea drawArea, SoundRegionItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showSoundRegions || IsHighlightOverriddenTrItem)
            {
                float angle = 0;
                float size = 4f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(thisLocation, "sound" + colors.nameExtension, angle, size, minPixelSize);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        private void drawPickup(DrawArea drawArea, PickupItem item, ColorScheme colors)
        {
            WorldLocation thisLocation = itemLocation(item);
            if (Properties.Settings.Default.showPickups || IsHighlightOverriddenTrItem)
            {
                float angle = 0;
                float size = 9f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(thisLocation, "pickup" + colors.nameExtension, angle, size, minPixelSize);
                closestTrItem.CheckMouseDistance(thisLocation, drawArea.mouseLocation, item);
            }
        }

        /// <summary>
        /// Utility method to translate the various coordinates within an Uid into a worldLocation
        /// </summary>
        /// <param name="uid">The MSTS Universal Identifier</param>
        /// <returns>The single-object worldLocation</returns>
        public static WorldLocation UiDLocation(UiD uid)
        {
            return new WorldLocation(uid.TileX, uid.TileZ, uid.X, 0, uid.Z);
        }


        /// <summary>
        /// Utility method to translate the various coordinates within an item into a worldLocation
        /// </summary>
        /// <param name="item">The item for which we want the worldLocation</param>
        /// <returns>The single-object worldLocation</returns>
        private static WorldLocation itemLocation(TrItem item)
        {
            return new WorldLocation(item.TileX, item.TileZ, item.X, item.Y, item.Z);
        }

        /// <summary>
        /// Returns length of a tracksection. 
        /// </summary>
        /// <remarks>Same method as in Traveller.cs, but that one is not public</remarks>
        public static float GetLength(TrackSection trackSection)
        {
            return trackSection.SectionCurve != null ? trackSection.SectionCurve.Radius * Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize.Length;
        }

        /// <summary>
        /// From the track node index find the tracknode, find its location, and prepare hightlighting
        /// </summary>
        /// <param name="tni">The trackNodeIndex identifying the tracknode</param>
        /// <returns>The eturn the (center) location of a tracknode or null if no tracknode could be identified</returns>
        public WorldLocation TrackNodeHighlightOverride(int tni)
        {
            if ((tni < 0) || (tni >= trackDB.TrackNodes.Length)) return null;
            TrackNode tn = trackDB.TrackNodes[tni];
            if (tn == null) return null;

            IsHighlightOverridden = true;
            if (tn.TrJunctionNode != null )
            {
                searchJunctionOrEnd = new CloseToMouseJunctionOrEnd(tn, "junction");
                return UiDLocation(tn.UiD);
            }

            if (tn.TrEndNode )
            {
                searchJunctionOrEnd = new CloseToMouseJunctionOrEnd(tn, "endnode");
                return UiDLocation(tn.UiD);
            }


            //vector node. 
            searchTrack = new CloseToMouseTrack(tsectionDat, tn);

            TrackNode nodeBehind = trackDB.TrackNodes[tn.TrPins[0].Link];
            TrackNode nodeAhead = trackDB.TrackNodes[tn.TrPins[1].Link];
            return trackLocation(tn, nodeBehind, nodeAhead);
        }

        /// <summary>
        /// From the track node index find the tracknode, find its location, and prepare hightlighting
        /// </summary>
        /// <param name="tni">The trackNodeIndex identifying the tracknode</param>
        /// <returns>The eturn the (center) location of a tracknode or null if no tracknode could be identified</returns>
        public WorldLocation TrackNodeHighlightOverrideRoad(int tni)
        {
            if (roadTrackDB == null) return null;
            if ((tni < 0) || (tni >= roadTrackDB.TrackNodes.Length)) return null;
            TrackNode tn = roadTrackDB.TrackNodes[tni];
            if (tn == null) return null;

            IsHighlightOverridden = true;
            
            if (tn.TrEndNode)
            {
                searchJunctionOrEnd = new CloseToMouseJunctionOrEnd(tn, "endnode");
                return UiDLocation(tn.UiD);
            }

            //vector node
            searchTrack = new CloseToMouseTrack(tsectionDat, tn);
            TrackNode nodeBehind = roadTrackDB.TrackNodes[tn.TrPins[0].Link];
            TrackNode nodeAhead = roadTrackDB.TrackNodes[tn.TrPins[1].Link];
            return trackLocation(tn, nodeBehind, nodeAhead);
        }

        /// <summary>
        /// return a single location that can be used to zoom around a track vector node
        /// </summary>
        /// <param name="tn">The trackNode self, assumed to be a vector node</param>
        /// <param name="nodeBehind">The junction or end node at the beginning of the vector node</param>
        /// <param name="nodeAhead">The junction or end node at the end of the vector node</param>
        /// <returns>The worldlocation describing the track</returns>
        /// <remarks>Obviously, a single location is always an estimate. Currently tries to find middle of end points</remarks>
        private WorldLocation trackLocation(TrackNode tn, TrackNode nodeBehind, TrackNode nodeAhead)
        {
            if (tn.TrVectorNode == null) return null;
            if (nodeBehind == null)
            {
                if (nodeAhead == null)
                {
                    // no junctions or end node at both sides. Oh, well, just take the first point
                    TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[0];
                    if (tvs == null) return null;
                    return new WorldLocation(tvs.TileX, tvs.TileZ, tvs.X, 0, tvs.Z);
                }
                else
                {
                    return UiDLocation(nodeAhead.UiD);
                }
            }
            else
            {
                if (nodeAhead == null)
                {
                    return UiDLocation(nodeBehind.UiD);
                }
                else
                {
                    return MiddleLocation(UiDLocation(nodeBehind.UiD), UiDLocation(nodeAhead.UiD));
                }
            }

        }

        /// <summary>
        /// Find the item with the given index. And if it exists, prepare for highlighting it
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns>The location of the found item (or null)</returns>
        public WorldLocation TrackItemHighlightOverride(int itemIndex)
        {
            IsHighlightOverriddenTrItem = false; // do not show all items, just yet. Only after CheckForHighlightOverride
            if ((itemIndex < 0) || (itemIndex >= trackDB.TrItemTable.Length)) return null;
            IsHighlightOverridden = true;
            TrItem item = trackDB.TrItemTable[itemIndex];
            searchTrItem = new CloseToMouseItem(item);
            return itemLocation(item);
        }

        /// <summary>
        /// Find the road item with the given index. And if it exists, prepare for highlighting it
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns>The location of the found item (or null)</returns>
        public WorldLocation TrackItemHighlightOverrideRoad(int itemIndex)
        {
            IsHighlightOverriddenTrItem = false; // do not show all items, just yet. Only after CheckForHighlightOverride
            if (roadTrackDB == null) return null;
            if ((itemIndex < 0) || (itemIndex >= roadTrackDB.TrItemTable.Length)) return null;
            IsHighlightOverridden = true;
            TrItem item = roadTrackDB.TrItemTable[itemIndex];
            searchTrItem = new CloseToMouseItem(item);
            return itemLocation(item);
        }

        /// <summary>
        /// We need to store the nodes/items that the user was searching for, so we can highlight them
        /// </summary>
        private static CloseToMouseJunctionOrEnd searchJunctionOrEnd;
        private static CloseToMouseTrack searchTrack;
        private static CloseToMouseItem searchTrItem;

        /// <summary>
        /// Clear all override highlights, returning to highlights based on mouse location
        /// </summary>
        public static void ClearHighlightOverrides()
        {
            IsHighlightOverriddenTrItem = false;
            IsHighlightOverridden = false;
            searchJunctionOrEnd = null;
            searchTrack = null;
            searchTrItem = null;
        }

        /// <summary>
        /// Check whether there is an hightlight override, and if there is make sure the item to highlighted is indeed used.
        /// </summary>
        void CheckForHighlightOverride()
        {
            IsHighlightOverriddenTrItem = (IsHighlightOverridden && (searchTrItem != null));
            if (!IsHighlightOverridden) return;
            if (searchJunctionOrEnd != null) closestJunctionOrEnd = searchJunctionOrEnd;
            if (searchTrItem != null) closestTrItem = searchTrItem;
            // To be sure the inset also shows the correct track, we need to make sure to make a deeper copy, instead
            // of changing only the reference.
            if (searchTrack != null) closestRailTrack = new CloseToMouseTrack(tsectionDat, searchTrack.TrackNode);
        }

        /// <summary>
        /// Return the location in the middle between the two given points.
        /// </summary>
        /// <param name="location1">Location of first point</param>
        /// <param name="location2">Location of second point</param>
        /// <returns>middle of both points</returns>
        /// <remarks>Should perhaps be in the WorldLocation class itself</remarks>
        static WorldLocation MiddleLocation(WorldLocation location1, WorldLocation location2)
        {
            int tileX = location1.TileX;
            int tileZ = location1.TileZ;
            WorldLocation location2Normalized = new WorldLocation(location2);
            location2Normalized.NormalizeTo(tileX, tileZ);
            Vector3 middleVector = (location1.Location + location2Normalized.Location) / 2;
            return new WorldLocation(tileX, tileZ, middleVector);
        }

        /// <summary>
        /// find the WorldLocation given the indexes to the vector node, vector section and distance into the section.
        /// </summary>
        /// <param name="trackNodeIndex"></param>
        /// <param name="trackVectorSectionIndex"></param>
        /// <param name="distanceAlongSection"></param>
        /// <param name="useRailTracks">Must we use rail or road tracks</param>
        public WorldLocation FindLocation(uint trackNodeIndex, int trackVectorSectionIndex, float distanceAlongSection, bool useRailTracks)
        {
            try
            {
                TrackNode tn = useRailTracks ?              
                    trackDB.TrackNodes[trackNodeIndex]: roadTrackDB.TrackNodes[trackNodeIndex];
                
                TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[trackVectorSectionIndex];
                WorldLocation location =  new WorldLocation(tvs.TileX, tvs.TileZ, tvs.X, tvs.Y, tvs.Z);
                TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);

                float cosA = (float)Math.Cos(tvs.AY);
                float sinA = (float)Math.Sin(tvs.AY);
                if (trackSection.SectionCurve == null)
                {
                    // note, angle is 90 degrees off, and different sign. 
                    // So Delta X = cos(90-A)=sin(A); Delta Y,Z = sin(90-A) = cos(A)    
                    location.Location.X += sinA * distanceAlongSection;
                    location.Location.Z += cosA * distanceAlongSection;
                }
                else
                {
                    int sign = (trackSection.SectionCurve.Angle > 0) ? -1 : 1;
                    float angleRadians = -distanceAlongSection/trackSection.SectionCurve.Radius;
                    float cosArotated = (float)Math.Cos(tvs.AY+sign*angleRadians);
                    float sinArotated = (float)Math.Sin(tvs.AY+sign*angleRadians);
                    float deltaX = sign*trackSection.SectionCurve.Radius * (cosA-cosArotated);
                    float deltaZ = sign*trackSection.SectionCurve.Radius * (sinA-sinArotated);
                    location.Location.X -= deltaX;
                    location.Location.Z += deltaZ;
                }
                return location;
            }
            catch
            {
                return null;
            }
        }
    }

}
