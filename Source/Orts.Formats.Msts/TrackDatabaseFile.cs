// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Orts.Parsers.Msts;
using ORTS.Common;
using Microsoft.Xna.Framework;
using Orts.Common;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// TDBFile is a representation of the .tdb file, that contains the track data base.
    /// The database contains two kinds of items: TrackNodes and TrItems (Track Items).
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable only used in using statement, known FcCop bug")]
    public class TrackDatabaseFile
    {
        /// <summary>
        /// Contains the Database with all the  tracks.
        /// </summary>
        public TrackDB TrackDB { get; set; }

        /// <summary>
        /// The .tdb file last modified time is stored for being able to validate derived files such as binary paths.
        /// </summary>
        public DateTime LastWriteTime { get; }

        /// <summary>
        /// Constructor from file
        /// </summary>
        /// <param name="filenamewithpath">Full file name of the .rdb file</param>
        public TrackDatabaseFile(string filenamewithpath)
        {        
            using (STFReader stf = new STFReader(filenamewithpath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackdb", ()=>{ TrackDB = new TrackDB(stf); }),
                });

            LastWriteTime = File.GetLastWriteTime(filenamewithpath);
        }

        /// <summary>
        /// Provide a link to the TrJunctionNode for the switch track with 
        /// the specified UiD on the specified tile.
        /// 
        /// Called by switch track shapes to determine the correct position of the points.
        /// </summary>
        /// <param name="tileX">X-value of the current Tile</param>
        /// <param name="tileZ">Z-value of the current Tile</param>
        /// <param name="worldId">world ID as defined in world file</param>
        /// <returns>The TrJunctionNode corresponding the the tile and worldID, null if not found</returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public TrJunctionNode GetTrJunctionNode(int tileX, int tileZ, int worldId)
        {
            foreach (TrackNode tn in TrackDB.TrackNodes)
            {
                if (tn != null && tn.TrJunctionNode != null)
                {
                    if (tileX == tn.UiD.WorldTileX && tileZ == tn.UiD.WorldTileZ && worldId == tn.UiD.WorldId)
                    {
                        return tn.TrJunctionNode;
                    }
                }
            }
            Trace.TraceWarning("{{TileX:{0} TileZ:{1}}} track node {2} could not be found in TDB", tileX, tileZ, worldId);
            return null;
        }
    }

    /// <summary>
    /// This class represents the Track Database.
    /// </summary>
    public class TrackDB
    {
        /// <summary>
        /// Array of all TrackNodes in the track database
        /// Warning, the first TrackNode is always null.
        /// </summary>
        public TrackNode[] TrackNodes; 

        /// <summary>
        /// Array of all Track Items (TrItem) in the road database
        /// </summary>
        public TrItem[] TrItemTable;

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public TrackDB(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tracknodes", ()=>{
                    stf.MustMatch("(");
                    int numberOfTrackNodes = stf.ReadInt(null);
                    TrackNodes = new TrackNode[numberOfTrackNodes + 1];
                    int idx = 1;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tracknode", ()=>{ TrackNodes[idx] = new TrackNode(stf, idx, numberOfTrackNodes); ++idx; }),
                    });
                }),
                new STFReader.TokenProcessor("tritemtable", ()=>{
                    stf.MustMatch("(");
                    int numberOfTrItems = stf.ReadInt(null);
                    TrItemTable = new TrItem[numberOfTrItems];
                    int idx = -1;
                    stf.ParseBlock(()=> ++idx == -1, new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("crossoveritem", ()=>{ TrItemTable[idx] = new CrossoverItem(stf,idx); }),
                        new STFReader.TokenProcessor("signalitem", ()=>{ TrItemTable[idx] = new SignalItem(stf,idx); }),
                        new STFReader.TokenProcessor("speedpostitem", ()=>{ TrItemTable[idx] = new SpeedPostItem(stf,idx); }),
                        new STFReader.TokenProcessor("platformitem", ()=>{ TrItemTable[idx] = new PlatformItem(stf,idx); }),
                        new STFReader.TokenProcessor("soundregionitem", ()=>{ TrItemTable[idx] = new SoundRegionItem(stf,idx); }),
                        new STFReader.TokenProcessor("emptyitem", ()=>{ TrItemTable[idx] = new EmptyItem(stf,idx); }),
                        new STFReader.TokenProcessor("levelcritem", ()=>{ TrItemTable[idx] = new LevelCrItem(stf,idx); }),
                        new STFReader.TokenProcessor("sidingitem", ()=>{ TrItemTable[idx] = new SidingItem(stf,idx); }),
                        new STFReader.TokenProcessor("hazzarditem", ()=>{ TrItemTable[idx] = new HazzardItem(stf,idx); }),
                        new STFReader.TokenProcessor("pickupitem", ()=>{ TrItemTable[idx] = new PickupItem(stf,idx); }),
                    });
                }),
            });
        }
        
        /// <summary>
        /// Find the index of the TrackNode
        /// </summary>
        /// <param name="targetTN">TrackNode for which you want the index</param>
        /// <returns>The index of the targetTN</returns>
        public int TrackNodesIndexOf(TrackNode targetTN)
        {
            for (int i = 0; i < TrackNodes.Length; ++i)
            {
                if (TrackNodes[i] == targetTN)
                {
                    //todo. This is only temporary. If we do not find an issue here soon, this whole method can be
                    //removed. Instead of a call to this method, we can simply used targetTN.Index instead.
                    if (i != targetTN.Index)
                    {
                        throw new InvalidOperationException("Program Bug: Index mismatch in track database");
                    }
                    return i;
                }
            }
            throw new InvalidOperationException("Program Bug: Can't Find Track Node");
        }

        /// <summary>
        /// Add a number of TrItems (Track Items), created outside of the file, to the table of TrItems.
        /// This will also set the ID of the TrItems (since that gives the index in that array)
        /// </summary>
        /// <param name="newTrItems">The array of new items.</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public void AddTrItems(TrItem[] newTrItems)
        {
            TrItem[] newTrItemTable;

            if (TrItemTable == null)
            {
                newTrItemTable = new TrItem[newTrItems.Length];
            }
            else
            {
                newTrItemTable = new TrItem[TrItemTable.Length + newTrItems.Length];
                TrItemTable.CopyTo(newTrItemTable, 0);
            }

            for (int i = 0; i < newTrItems.Length; i++)
            {
                int newId = i + TrItemTable.Length;
                newTrItems[i].TrItemId = (uint) newId;
                newTrItemTable[newId] = newTrItems[i];
            }

            TrItemTable = newTrItemTable;
        }

        public void AddTrNodesToPointsOnApiMap(InfoApiMap infoApiMap)
        {
            foreach (TrackNode trackNode in TrackNodes)
            {
                if (trackNode != null)
                {
                    try 
                    {
                        if (trackNode.UiD != null)
                        {
                            infoApiMap.AddToPointOnApiMap(
                                trackNode.UiD.TileX, trackNode.UiD.TileZ,
                                trackNode.UiD.X, trackNode.UiD.Y, trackNode.UiD.Z,
                                "red", TypeOfPointOnApiMap.Track, "track");
                        }

                        if ((trackNode.TrJunctionNode != null) && (trackNode.TrJunctionNode.TN.UiD != null))
                        {
                            infoApiMap.AddToPointOnApiMap(
                                trackNode.TrJunctionNode.TN.UiD.TileX, trackNode.TrJunctionNode.TN.UiD.TileZ,
                                trackNode.TrJunctionNode.TN.UiD.X, trackNode.TrJunctionNode.TN.UiD.Y, trackNode.TrJunctionNode.TN.UiD.Z,
                                "red", TypeOfPointOnApiMap.Track, "track");
                        }

                        if ((trackNode.TrVectorNode != null) && (trackNode.TrVectorNode.TrVectorSections != null))
                        {
                            bool first = true;
                            LatLon latLonFrom = new LatLon(0, 0);
                            TrVectorSection trVectorSectionLast = null;
                            foreach (TrVectorSection trVectorSection in trackNode.TrVectorNode.TrVectorSections)
                            {
                                LatLon latLonTo = InfoApiMap.ConvertToLatLon(trVectorSection.TileX, trVectorSection.TileZ,
                                    trVectorSection.X, trVectorSection.Y, trVectorSection.Z);
                                infoApiMap.AddToPointOnApiMap(latLonTo, "red", TypeOfPointOnApiMap.Track, "track");
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    infoApiMap.AddToLineOnApiMap(latLonFrom, latLonTo);
                                }
                                latLonFrom = latLonTo;
                                trVectorSectionLast = trVectorSection;
                            }
                            if (trVectorSectionLast != null)
                            {
                                if (trackNode.TrPins.Length == 2)
                                {
                                    int link = trackNode.TrPins[1].Link;
                                    LatLon latLonTo = InfoApiMap.ConvertToLatLon(TrackNodes[link].UiD.TileX, TrackNodes[link].UiD.TileZ,
                                        TrackNodes[link].UiD.X, TrackNodes[link].UiD.Y, TrackNodes[link].UiD.Z);
                                    infoApiMap.AddToLineOnApiMap(latLonFrom, latLonTo);
                                }
                            }
                        }

                        if (trackNode.TrEndNode)
                        {
                            LatLon latLonFrom = InfoApiMap.ConvertToLatLon(
                                trackNode.UiD.TileX, trackNode.UiD.TileZ,
                                trackNode.UiD.X, trackNode.UiD.Y, trackNode.UiD.Z);
                            int lastIndex = TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections.Length - 1;
                            LatLon latLonTo = InfoApiMap.ConvertToLatLon(
                                TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections[lastIndex].TileX,
                                TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections[lastIndex].TileZ,
                                TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections[lastIndex].X,
                                TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections[lastIndex].Y,
                                TrackNodes[trackNode.TrPins[0].Link].TrVectorNode.TrVectorSections[lastIndex].Z);
                            infoApiMap.AddToLineOnApiMap(latLonFrom, latLonTo);
                        }
                    }
                    catch (Exception e)
                    {
                        // just skip the trackNode with a problem,
                        // better to skip this trackNode than to abort Open Rails
                    }
                }
            }
        }

        public void AddTrItemsToPointsOnApiMap(InfoApiMap infoApiMap)
        {
            foreach (TrItem trItem in TrItemTable)
            {
                if ((trItem != null) && (trItem.TileX != 0))
                {
                    string itemType = trItem.ItemType.ToString().ToLower();
                    if (itemType.StartsWith("tr"))
                    {
                        itemType = itemType.Substring(2);
                    }
                    if (itemType != "xing")
                    {
                        if (trItem.ItemName == null)
                        {
                            infoApiMap.AddToPointOnApiMap(
                            trItem.TileX, trItem.TileZ,
                            trItem.X, trItem.Y, trItem.Z,
                            "blue", TypeOfPointOnApiMap.Rest, $"{itemType}");
                        }
                        else
                        {
                            infoApiMap.AddToPointOnApiMap(
                            trItem.TileX, trItem.TileZ,
                            trItem.X, trItem.Y, trItem.Z,
                            "green", TypeOfPointOnApiMap.Named, $"{trItem.ItemName.Replace("'", "")}, {itemType}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a TrackNode. This is either an endNode, a junctionNode, or a vectorNode. 
    /// A VectorNode is a connection between two junctions or endnodes.
    /// </summary>
    public class TrackNode
    {
        /// <summary>
        /// If this is a junction, this contains a link to a TrJunctionNode that contains the details about the junction.
        /// null otherwise.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public TrJunctionNode TrJunctionNode { get; set; }

        /// <summary>
        /// If this is a vector nodes, this contains a link to a TrVectorNode that contains the details about the vector
        /// null otherwise.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public TrVectorNode TrVectorNode { get; set; }
        
        /// <summary>
        /// True when this TrackNode has nothing else connected to it (that is, it is
        /// a buffer end or an unfinished track) and trains cannot proceed beyond here.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public bool TrEndNode { get; set; }

        /// <summary>'Universal Id', containing location information. Only provided for TrJunctionNode and TrEndNode type of TrackNodes</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public UiD UiD { get; set; }
        /// <summary>The array containing the TrPins (Track pins), which are connections to other tracknodes</summary>
        public TrPin[] TrPins;
        /// <summary>Number of outgoing pins (connections to other tracknodes)</summary>
        public uint Inpins { get; set; }
        /// <summary>Number of outgoing pins (connections to other tracknodes)</summary>
        public uint Outpins { get; set; }

        /// <summary>The index in the array of tracknodes.</summary>
        public uint Index { get; set; }

        /// <summary>??? (needed for ActivityEditor, but not used here, so why is it defined here?)</summary>
        public bool Reduced { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this node in the list of TrackNodes</param>
        /// <param name="numberOfTrackNodes">The number of Tracknodes that we should have, to make sure we do not overstep bounds</param>
        public TrackNode(STFReader stf, int idx, int numberOfTrackNodes)
        {
            stf.MustMatch("(");
            Index = stf.ReadUInt(null);
            Debug.Assert(idx == Index, "TrackNode Index Mismatch");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = new UiD(stf); }),
                new STFReader.TokenProcessor("trjunctionnode", ()=>{ TrJunctionNode = new TrJunctionNode(stf, idx); TrJunctionNode.TN = this; }),
                new STFReader.TokenProcessor("trvectornode", ()=>{ TrVectorNode = new TrVectorNode(stf); }),
                new STFReader.TokenProcessor("trendnode", ()=>{ TrEndNode = true; stf.SkipBlock(); }),
                new STFReader.TokenProcessor("trpins", ()=>{
                    stf.MustMatch("(");
                    Inpins = stf.ReadUInt(null);
                    Outpins = stf.ReadUInt(null);
                    TrPins = new TrPin[Inpins + Outpins];
                    for (int i = 0; i < Inpins + Outpins; ++i)
                    {
                        stf.MustMatch("TrPin");
                        TrPins[i] = new TrPin(stf);
                        if (TrPins[i].Link <= 0 || TrPins[i].Link > numberOfTrackNodes)
                            STFException.TraceWarning(stf, String.Format(System.Globalization.CultureInfo.CurrentCulture,
                                "Track node {0} pin {1} has invalid link to track node {2}", Index, i, TrPins[i].Link));
                    }
                    stf.SkipRestOfBlock();
                }),
            });
            // TODO We assume there is only 2 outputs to each junction
            var expectedPins = TrJunctionNode != null ? new[] { 3, 1, 2 } : TrVectorNode != null ? new[] { 2, 1, 1 } : TrEndNode ? new[] { 1, 1, 0 } : new[] { 0, 0, 0 };
            if (TrPins.Length != expectedPins[0])
                Trace.TraceWarning("Track node {0} has unexpected number of pins; expected {1}, got {2}", Index, expectedPins[0], TrPins.Length);
            if (Inpins != expectedPins[1])
                Trace.TraceWarning("Track node {0} has unexpected number of input pins; expected {1}, got {2}", Index, expectedPins[1], TrPins.Length);
            if (Outpins != expectedPins[2])
                Trace.TraceWarning("Track node {0} has unexpected number of output pins; expected {1}, got {2}", Index, expectedPins[2], TrPins.Length);
        }

        /// <summary>
        /// Create a trackNode from a another trackNode, by copying all members and arrays.
        /// Not a deep copy, because various arrays in the structure are copied shallow (cloned).
        /// </summary>
        /// <param name="otherNode">The other node to copy from.</param>
        public TrackNode(TrackNode otherNode)
        {
            if (otherNode.TrJunctionNode != null)
            {
                this.TrJunctionNode = new TrJunctionNode(otherNode.TrJunctionNode);
                this.TrJunctionNode.TN = this; // make the back-reference correct again.
            }
            if (otherNode.TrVectorNode != null)
            {
                this.TrVectorNode = new TrVectorNode(otherNode.TrVectorNode);
            }
            this.TrEndNode = otherNode.TrEndNode;
            this.Inpins = otherNode.Inpins;
            this.Outpins = otherNode.Outpins;
            this.Index = otherNode.Index;
            this.UiD = otherNode.UiD;
            this.Reduced = otherNode.Reduced;
            this.TrPins = (TrPin[])otherNode.TrPins.Clone();
        }

        /// <summary>
        /// List of references to Track Circuit sections
        /// </summary>
        public TrackCircuitXRefList TCCrossReference;

    }

    #region class TrPin
    /// <summary>
    /// Represents a pin, being the link from a tracknode to another. 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    [DebuggerDisplay("\\{MSTS.TrPin\\} Link={Link}, Dir={Direction}")]
    public class TrPin
    {
        /// <summary>Index of the tracknode connected to the parent of this pin</summary>
        public int Link { get; set; }
        /// <summary>In case a connection is made to a vector node this determines the side of the vector node that is connected to</summary>
        public int Direction { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public TrPin(STFReader stf)
        {
            stf.MustMatch("(");
            Link = stf.ReadInt(null);
            Direction = stf.ReadInt(null);
            stf.SkipRestOfBlock();
        }

        /// <summary>
        /// Default (empty) constructor 
        /// </summary>
        public TrPin() {}

        /// <summary>
        /// Create a shallow copy of the current TrPin
        /// </summary>
        /// <returns>a new object</returns>
        public TrPin Copy()
        {
            return (TrPin)this.MemberwiseClone();
        }
    }
    #endregion

    /// <summary>
    /// Contains the location and initial direction (as an angle in 3 dimensions) of a node (junction or end),
    /// as well as a cross reference to the entry in the world file
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    [DebuggerDisplay("\\{MSTS.UiD\\} ID={WorldID}, TileX={TileX}, TileZ={TileZ}, X={X}, Y={Y}, Z={Z}, AX={AX}, AY={AY}, AZ={AZ}, WorldX={WorldTileX}, WorldZ={WorldTileZ}")]
    public class UiD
    {
        /// <summary>X-value of the tile where the node is located</summary>
        public int TileX { get; set; }
        /// <summary>Z-value of the tile where the node is located</summary>
        public int TileZ { get; set; }
        /// <summary>X-value within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float X { get; set; }
        /// <summary>Y-value (height) within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Y { get; set; }
        /// <summary>Z-value within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Z { get; set; }
        /// <summary>Angle around X-axis for describing initial direction of the node</summary>
        public float AX { get; set; }
        /// <summary>Angle around Y-axis for describing initial direction of the node</summary>
        public float AY { get; set; }
        /// <summary>Angle around Z-axis for describing initial direction of the node</summary>
        public float AZ { get; set; }

        /// <summary>Cross-reference to worldFile: X-value of the tile</summary>
        public int WorldTileX { get; set; }
        /// <summary>Cross-reference to worldFile: Y-value of the tile</summary>
        public int WorldTileZ { get; set; }
        /// <summary>Cross-reference to worldFile: World ID</summary>
        public int WorldId { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public UiD(STFReader stf)
        {
            stf.MustMatch("(");
            WorldTileX = stf.ReadInt(null);
            WorldTileZ = stf.ReadInt(null);
            WorldId = stf.ReadInt(null);
            stf.ReadInt(null);
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            AX = stf.ReadFloat(STFReader.UNITS.None, null);
            AY = stf.ReadFloat(STFReader.UNITS.None, null);
            AZ = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
        
        /// <summary>
        /// Constructor from a vector section
        /// </summary>
        /// <param name="vectorSection">The vectorSection that is used to define the UiD (a.o. location)</param>
        public UiD(TrVectorSection vectorSection)
        {
            WorldTileX = vectorSection.TileX;
            WorldTileZ = vectorSection.TileZ;
            WorldId = (int)vectorSection.SectionIndex;
            TileX = vectorSection.TileX;
            TileZ = vectorSection.TileZ;
            X = vectorSection.X;
            Y = vectorSection.Y;
            Z = vectorSection.Z;
            AX = vectorSection.AX;
            AY = vectorSection.AY;
            AZ = vectorSection.AZ;
        }
    }

    /// <summary>
    /// Describes details of a junction
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    [DebuggerDisplay("\\{MSTS.TrJunctionNode\\} SelectedRoute={SelectedRoute}, ShapeIndex={ShapeIndex}")]
    public class TrJunctionNode
    {
        /// <summary>
        /// The route of a switch that is currently in use.
        /// </summary>
        public int SelectedRoute { get; set; }
        
        /// <summary>
        /// Reference to the parent trackNode
        /// </summary>
        public TrackNode TN { get; set; }

        /// <summary>
        /// ??? This is probably intended to be the index in the list of TrackNodes, but it is not used anywhere
        /// Perhaps in ActivityEditor? but is it consistent?
        /// </summary>
        public int Idx { get; private set; }

        /// <summary>
        /// Index to the shape that actually describes the looks of this switch
        /// </summary>
        public uint ShapeIndex { get; set; }

        /// <summary>The angle of this junction</summary>
        private double angle = double.MaxValue;
        /// <summary>The angle has been set through section file</summary>
        private bool AngleComputed; //

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this node in the list of TrackNodes</param>
        public TrJunctionNode(STFReader stf, int idx)
        {
            Idx = idx;
            stf.MustMatch("(");
            stf.ReadString();
            ShapeIndex = stf.ReadUInt(null);
            stf.SkipRestOfBlock();
        }

        /// <summary>
        /// Create a junctionNode from a another junctionNode, by copying all members.
        /// Not a deep copy, because the backreference to the parent tracknode is not copied
        /// </summary>
        /// <param name="otherNode">The other node to copy from.</param>
        public TrJunctionNode(TrJunctionNode otherNode)
        {
            //todo: if the idx gives the index in the array of tracknodes, it should not be a copy of another node.
            this.Idx = otherNode.Idx;
            this.ShapeIndex = otherNode.ShapeIndex;
            this.SelectedRoute = otherNode.SelectedRoute;
            this.AngleComputed = otherNode.AngleComputed;
            this.angle = otherNode.angle;
        }

        /// <summary>
        /// Calculate the angle (direction in 2D) of the current junction (result will be cached).
        /// </summary>
        /// <param name="tsectionDat">The datafile with all the track sections</param>
        /// <returns>The angle calculated</returns>
        public double GetAngle(TrackSectionsFile tsectionDat)
        {
            if (AngleComputed) { return angle; }

            AngleComputed = true;
            try //so many things can be in conflict for trackshapes, tracksections etc.
            {
                TrackShape trackShape = tsectionDat.TrackShapes.Get(ShapeIndex);
                SectionIdx[] SectionIdxs = trackShape.SectionIdxs;

                for (int index = 0; index <= SectionIdxs.Length-1 ; index++)
                {
                    if (index == trackShape.MainRoute) continue;
                    uint[] sections = SectionIdxs[index].TrackSections;

                    for (int i = 0; i < sections.Length; i++)
                    {
                        uint sid = SectionIdxs[index].TrackSections[i];
                        TrackSection section = tsectionDat.TrackSections[sid];

                        if (section.SectionCurve != null)
                        {
                            angle = section.SectionCurve.Angle;
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }

            return angle;
        }
    }

    /// <summary>
    /// Describes the details of a vectorNode, a connection between two junctions (or endnodes).
    /// A vectorNode itself is made up of various sections. The begin point of each of these sections
    /// is stored (as well as its direction). As a result, VectorNodes have a direction.
    /// Furthermore, a number of TrItems (Track Items) can be located on the vector nodes.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    public class TrVectorNode
    {
        /// <summary>Array of sections that together form the vectorNode</summary>
        public TrVectorSection[] TrVectorSections;
        /// <summary>Array of indexes of TrItems (track items) that are located on this vectorNode</summary>
        public int[] TrItemRefs;
        /// <summary>The amount of TrItems in TrItemRefs</summary>
        public int NoItemRefs { get; set; } // it would have been better to use TrItemRefs.Length instead of keeping count ourselve

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public TrVectorNode(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("trvectorsections", ()=>{
                    stf.MustMatch("(");
                    int numberOfVectorSections = stf.ReadInt(null);
                    TrVectorSections = new TrVectorSection[numberOfVectorSections];
                    for (int i = 0; i < numberOfVectorSections; ++i)
                        TrVectorSections[i] = new TrVectorSection(stf);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("tritemrefs", ()=>{
                    stf.MustMatch("(");
                    NoItemRefs = stf.ReadInt(null);
                    TrItemRefs = new int[NoItemRefs];
                    int refidx = 0;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tritemref", ()=>{
                            if (refidx >= NoItemRefs)
                                STFException.TraceWarning(stf, "Skipped extra TrItemRef");
                            else
                                TrItemRefs[refidx++] = stf.ReadIntBlock(null);
                        }),
                    });
                    if (refidx < NoItemRefs)
                        STFException.TraceWarning(stf, (NoItemRefs - refidx).ToString(System.Globalization.CultureInfo.CurrentCulture)
                            + " missing TrItemRef(s)");
                }),
            });
        }

        /// <summary>
        /// Create a vectorNode from a another VectorNode, by copying all members and arrays.
        /// Not a deep copy, because the arrays are copied shallow.
        /// </summary>
        /// <param name="otherNode">The other node to copy from.</param>
        public TrVectorNode(TrVectorNode otherNode)
        {
            this.NoItemRefs = otherNode.NoItemRefs;
            if (otherNode.TrItemRefs != null)
                this.TrItemRefs = (int[])otherNode.TrItemRefs.Clone();
            if (otherNode.TrVectorSections != null)
                this.TrVectorSections = (TrVectorSection[])otherNode.TrVectorSections.Clone();
        }

        /// <summary>
        /// Get the index of a vector section in the array of vectorsections 
        /// </summary>
        /// <param name="targetTvs">The vector section for which the index is needed</param>
        /// <returns>the index of the vector section</returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public int TrVectorSectionsIndexOf(TrVectorSection targetTvs)
        {
            for (int i = 0; i < TrVectorSections.Length; ++i)
            {
                if (TrVectorSections[i] == targetTvs)
                {
                    return i;
                }
            }
            throw new InvalidOperationException("Program Bug: Can't Find TVS");
        }

        /// <summary>
        /// Add a reference to a new TrItem to the already existing TrItemRefs.
        /// </summary>
        /// <param name="newTrItemRef">The reference to the new TrItem</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public void AddTrItemRef(int newTrItemRef)
        {
            int[] newTrItemRefs = new int[NoItemRefs + 1];
            TrItemRefs.CopyTo(newTrItemRefs, 0);
            newTrItemRefs[NoItemRefs] = newTrItemRef;
            TrItemRefs = newTrItemRefs; //use the new item lists for the track node
            NoItemRefs++;
        }
    }

    /// <summary>
    /// Describes a single section in a vector node. 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    public class TrVectorSection
    {
        /// <summary>First flag. Not completely clear, usually 0, - may point to the connecting pin entry in a junction. Sometimes 2</summary>
        public int Flag1 { get; set; }
        /// <summary>Second flag. Not completely clear, usually 1, but set to 0 when curve track is flipped around. Sometimes 2</summary>
        public int Flag2 { get; set; }
        /// <summary>Index of the track section in Tsection.dat</summary>
        public uint SectionIndex { get; set; }
        /// <summary>Index to the shape from Tsection.dat</summary>
        public uint ShapeIndex { get; set; }
        /// <summary>X-value of the location-tile</summary>
        public int TileX { get; set; }
        /// <summary>Z-value of the location-tile</summary>
        public int TileZ { get; set; }
        /// <summary>X-value within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float X { get; set; }
        /// <summary>Y-value (height) within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Y { get; set; }
        /// <summary>Z-value within the tile where the node is located</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Z { get; set; }
        /// <summary>Angle around X-axis for describing initial direction of the node</summary>
        public float AX { get; set; }
        /// <summary>Angle around Y-axis for describing initial direction of the node</summary>
        public float AY { get; set; }
        /// <summary>Angle around Z-axis for describing initial direction of the node</summary>
        public float AZ { get; set; }

        //The following items are related to super elevation
        /// <summary>The index to the worldFile</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public uint WorldFileUiD { get; set; }
        /// <summary>The TileX in the WorldFile</summary>
        public int WFNameX { get; set; }
        /// <summary>The TileZ in the WorldFile</summary>
        public int WFNameZ { get; set; }
        /// <summary>The (super)elevation at the start</summary>
        public float StartElev { get; set; }
        /// <summary>The (super)elevation at the end</summary>
        public float EndElev { get; set; }
        /// <summary>The maximum (super) elevation</summary>
        public float MaxElev { get; set; }

        /// <summary>??? (needed for ActivityEditor, but not used here, so why is it defined here?)</summary>
        public bool Reduced { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public TrVectorSection(STFReader stf)
        {
            SectionIndex = stf.ReadUInt(null);
            ShapeIndex = stf.ReadUInt(null);
            WFNameX = stf.ReadInt(null);// worldfilenamex
            WFNameZ = stf.ReadInt(null);// worldfilenamez
            WorldFileUiD = stf.ReadUInt(null); // UID in worldfile
            Flag1 = stf.ReadInt(null); // 0
            Flag2 = stf.ReadInt(null); // 1
            stf.ReadString(); // 00 
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            AX = stf.ReadFloat(STFReader.UNITS.None, null);
            AY = stf.ReadFloat(STFReader.UNITS.None, null);
            AZ = stf.ReadFloat(STFReader.UNITS.None, null);
        }

        /// <summary>
        /// Overriding the ToString, which makes it easier to debug
        /// </summary>
        /// <returns>String giving info on this section</returns>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{{TileX:{0} TileZ:{1} X:{2} Y:{3} Z:{4} UiD:{5} Section:{6} Shape:{7}}}", WFNameX, WFNameZ, X, Y, Z, WorldFileUiD, SectionIndex, ShapeIndex);
        }
    }

    /// <summary>
    /// Describes a Track Item, that is an item located on the track that interacts with the train or train operations
    /// This is a base class. 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    public abstract class TrItem
    {
        /// <summary>
        /// The name of the item (used for the label shown by F6)
        /// </summary>
        public string ItemName { get; set; }

        //todo. 4 things: first the enum should be outside of the class. second, the casing of the names is really wrong.
        //third, since it is already a TrItemType (note casing, corresponding to MSTS), 'Empty' should be enough, no need for an additional tr.
        //fourth, since the subclass is already a type definition, it is not needed to have an enum on top of it. It should be doable by 
        //using syntax like "trItem is SignalItem" instead of "trItem.ItemType == TrItem.trItemType.trSIGNAL" or other 
        //decent object-oriented inheritance features.
        /// <summary>
        /// Describes the various types of Track Items
        /// </summary>
        public enum trItemType
        {   
            /// <summary>empty item</summary>
            trEMPTY, // the first, so translates to '0', so this is the default.
            /// <summary>A place where two tracks cross over each other</summary>
            trCROSSOVER,
            /// <summary>A signal</summary>
            trSIGNAL,
            /// <summary>A post with either speed or distance along track</summary>
            trSPEEDPOST,
            /// <summary>A platform</summary>
            trPLATFORM,
            /// <summary>A location where a sound can be triggerd</summary>
            trSOUNDREGION,
            /// <summary>A crossing between rail and road</summary>
            trXING,
            /// <summary>A siding</summary>
            trSIDING,
            /// <summary>A hazard, meaning something dangerous on or next to track</summary>
            trHAZZARD,
            /// <summary>A pickup of fuel, water, ...</summary>
            trPICKUP,
            /// <summary>The place where cars are appear of disappear</summary>
            trCARSPAWNER
        }

        /// <summary>Type of track item</summary>
        public trItemType ItemType { get; set; }
        /// <summary>Id if track item</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public uint TrItemId { get; set; }
        /// <summary>X-value of world tile</summary>
        public int TileX { get; set; }
        /// <summary>Z-value of world tile</summary>
        public int TileZ { get; set; }
        /// <summary>X-location within world tile (tracknode, not shape)</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float X { get; set; }
        /// <summary>X-location within world tile (tracknode, not shape)</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Y { get; set; }
        /// <summary>X-location within world tile (tracknode, not shape)</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Name is meaningful enough")]
        public float Z { get; set; }
        /// <summary>Appears to be a copy of tileX in Sdata, but only for X and Z</summary>
        public int TilePX { get; set; }
        /// <summary>Appears to be a copy of tileZ in Sdata, but only for X and Z</summary>
        public int TilePZ { get; set; }
        /// <summary>Appears to be a copy of X in Sdata, but only for X and Z</summary>
        public float PX { get; set; }
        /// <summary>Appears to be a copy of X in Sdata, but only for X and Z</summary>
        public float PZ { get; set; }
        /// <summary>Distance of a track item along its containing track section and measured from the origin of the section</summary>
        public float SData1 { get; set; }
        /// <summary>Extra data 2</summary>
        public string SData2 { get; set; }

        /// <summary>
        /// Base constructor
        /// </summary>
        protected TrItem()
        {
            ItemType = trItemType.trEMPTY;
        }

        /// <summary>
        /// Reads the ID from filestream
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        protected void ParseTrItemID(STFReader stf, int idx)
        {
            stf.MustMatch("(");
            TrItemId = stf.ReadUInt(null);
            Debug.Assert(idx == TrItemId, "Index Mismatch");
            stf.SkipRestOfBlock();
        }
        
        /// <summary>
        /// Reads the Rdata from filestream
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        protected void TrItemRData(STFReader stf)
        {
            stf.MustMatch("(");
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            stf.SkipRestOfBlock();
        }

        /// <summary>
        /// Reads the PData from filestream
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        protected void TrItemPData(STFReader stf)
        {
            stf.MustMatch("(");
            PX = stf.ReadFloat(STFReader.UNITS.None, null);
            PZ = stf.ReadFloat(STFReader.UNITS.None, null);
            TilePX = stf.ReadInt(null);
            TilePZ = stf.ReadInt(null);
            stf.SkipRestOfBlock();
        }

        /// <summary>
        /// Reads the SData from filestream
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        protected void TrItemSData(STFReader stf)
        {
            stf.MustMatch("(");
            SData1 = stf.ReadFloat(STFReader.UNITS.None, null);
            SData2 = stf.ReadString();
            stf.SkipRestOfBlock();
        }
    } // TrItem

    /// <summary>
    /// Describes a cross-over track item
    /// </summary>
    public class CrossoverItem : TrItem
    {
        /// <summary>Index to the tracknode</summary>
        public uint TrackNode { get; set; }
        /// <summary>Index to the shape ID</summary>
        public uint ShapeId { get; set; }
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public CrossoverItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trCROSSOVER;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("crossovertritemdata", ()=>{
                    stf.MustMatch("(");
                    TrackNode = stf.ReadUInt(null);
                    ShapeId = stf.ReadUInt(null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    /// <summary>
    /// Describes a signal item
    /// </summary>
    public class SignalItem : TrItem
    {
        /// <summary>
        /// Struct to describe details of the signal for junctions
        /// </summary>
        public struct StrTrSignalDir
        {
            /// <summary>Index to the junction track node</summary>
            public uint TrackNode { get; set; }
            /// <summary>Used with junction signals, appears to be either 1 or 0</summary>
            public uint Sd1 { get; set; }
            /// <summary>Used with junction signals, appears to be either 1 or 0</summary>
            public uint LinkLRPath { get; set; }
            /// <summary>Used with junction signals, appears to be either 1 or 0</summary>
            public uint Sd3 { get; set; }
        }

        /// <summary>Set to  00000001 if junction link set</summary>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", Justification = "It simply describesr real flags in MSTS file")]
        public string Flags1 { get; set; }
        /// <summary>0 or 1 depending on which way signal is facing</summary>
        public uint Direction { get; set; }
        /// <summary>index to Sigal Object Table</summary>
        public int SigObj { get; set; }
        /// <summary>Signal Data 1</summary>
        public float SigData1 { get; set; }
        /// <summary>Type of signal</summary>
        public string SignalType { get; set; }
        /// <summary>Number of junction links</summary>
        public uint NoSigDirs { get; set; }
        /// <summary></summary>
        public StrTrSignalDir[] TrSignalDirs;

        /// <summary>Get the direction the signal is NOT facing</summary>
        public int ReverseDirection
        {
            get { return Direction == 0 ? 1 : 0; }
        }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public SignalItem(STFReader stf, int idx)
        {
            SigObj = -1;
            ItemType = trItemType.trSIGNAL;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("trsignaltype", ()=>{
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    Direction = stf.ReadUInt(null);
                    SigData1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    SignalType = stf.ReadString().ToLowerInvariant();
                    // To do get index to Sigtypes table corresponding to this sigmal
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("trsignaldirs", ()=>{
                    stf.MustMatch("(");
                    NoSigDirs = stf.ReadUInt(null);
                    TrSignalDirs = new StrTrSignalDir[NoSigDirs];
                    int sigidx = 0;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trsignaldir", ()=>{
                            if (sigidx >= NoSigDirs)
                                STFException.TraceWarning(stf, "Skipped extra TrSignalDirs");
                            else
                            {
                                TrSignalDirs[sigidx]=new StrTrSignalDir();
                                stf.MustMatch("(");
                                TrSignalDirs[sigidx].TrackNode = stf.ReadUInt(null);
                                TrSignalDirs[sigidx].Sd1 = stf.ReadUInt(null);
                                TrSignalDirs[sigidx].LinkLRPath = stf.ReadUInt(null);
                                TrSignalDirs[sigidx].Sd3 = stf.ReadUInt(null);
                                stf.SkipRestOfBlock();
                                sigidx++;
                            }
                        }),
                    });
                    if (sigidx < NoSigDirs)
                        STFException.TraceWarning(stf, (NoSigDirs - sigidx).ToString(System.Globalization.CultureInfo.CurrentCulture)
                            + " missing TrSignalDirs(s)");
                }),
            });
        }
    }

    /// <summary>
    /// Describes SpeedPost of MilePost (could be Kilometer post as well)
    /// </summary>
    public class SpeedPostItem : TrItem
    {
        /// <summary>Flags from raw file describing exactly what this is.</summary>
        private uint Flags { get; set; }
        /// <summary>true to be milepost</summary>
        public bool IsMilePost { get; set; }
        /// <summary>speed warning</summary>
        public bool IsWarning { get; set; }
        /// <summary>speed limit</summary>
        public bool IsLimit { get; set; }
        /// <summary>speed resume sign (has no speed defined!)</summary>
        public bool IsResume { get; set; }
        /// <summary>is passenger speed limit</summary>
        public bool IsPassenger { get; set; }
        /// <summary>is freight speed limit</summary>
        public bool IsFreight { get; set; }
        /// <summary>is the digit in MPH or KPH</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Preference to keep units in capitals")]
        public bool IsMPH { get; set; }
        /// <summary>show numbers instead of KPH, like 5 means 50KMH</summary>
        public bool ShowNumber { get; set; }
        /// <summary>if ShowNumber is true and this is set, will show 1.5 as for 15KMH</summary>
        public bool ShowDot { get; set; }
        /// <summary>Or distance if mile post.</summary>
        public float SpeedInd { get; set; }

        /// <summary>index to Signal Object Table</summary>
        public int SigObj { get; set; }
        /// <summary>speedpost (normalized) angle</summary>
        public float Angle { get; set; }
        /// <summary>derived direction relative to track</summary>
        public int Direction { get; set; }
        /// <summary>number to be displayed if ShowNumber is true</summary>
        public int DisplayNumber { get; set; }

        /// <summary>Get the direction the signal is NOT facing</summary>
        public int ReverseDirection
        {
            get { return Direction == 0 ? 1 : 0; }
        }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public SpeedPostItem(STFReader stf, int idx)
        {
            SigObj = -1;
            ItemType = trItemType.trSPEEDPOST;
            stf.MustMatch("(");
			stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("speedposttritemdata", ()=>{
                    stf.MustMatch("(");
                    Flags = stf.ReadUInt(null);
					if ((Flags & 1) != 0) IsWarning = true;
					if ((Flags & (1 << 1)) != 0) IsLimit = true;
					if (!IsWarning && !IsLimit) {
						IsMilePost = true;
					}
					else {
						if (IsWarning && IsLimit)
						{
							IsWarning = false;
							IsResume = true;
						}

						if ((Flags & (1 << 5)) != 0) IsPassenger = true;
						if ((Flags & (1 << 6)) != 0) IsFreight = true;
						if ((Flags & (1 << 7)) != 0) IsFreight = IsPassenger = true;
						if ((Flags & (1 << 8)) != 0) IsMPH = true;
						if ((Flags & (1 << 4)) != 0) {
							ShowNumber = true;
							if ((Flags & (1 << 9)) != 0) ShowDot = true;
						}
					}


                    //  The number of parameters depends on the flags seeting
                    //  To do: Check flags seetings and parse accordingly.
		            if (!IsResume)
		            {
                        //SpeedInd = stf.ReadFloat(STFReader.UNITS.None, null);
                        if (IsMilePost && ((Flags & (1 << 9)) == 0)) SpeedInd = (float)Math.Truncate(stf.ReadDouble(null));
                        else SpeedInd = stf.ReadFloat(STFReader.UNITS.None, null);
		            }

    		        if (ShowNumber)
		            {
			            DisplayNumber = stf.ReadInt(null);
		            }
                    
			        Angle = MathHelper.WrapAngle(stf.ReadFloat(STFReader.UNITS.None, null));

                    stf.SkipRestOfBlock();
                }),
            });
        }

        // used as base for TempSpeedPostItem
        public SpeedPostItem()
        { }
    }

    public class TempSpeedPostItem : SpeedPostItem
    {      
        /// <summary>
        /// Constructor for creating a speedpost from activity speed restriction zone
        /// </summary>
        /// <param name="routeFile">The routeFile with relevant data about speeds</param>
        /// <param name="position">Position/location of the speedposts</param>
        /// <param name="isStart">Is this the start of a speed zone?</param>
        /// 
        public WorldPosition WorldPosition;

        public TempSpeedPostItem(Tr_RouteFile routeFile, Position position,  bool isStart, WorldPosition worldPosition, bool isWarning)
        {
            // TrItemId needs to be set later
            ItemType = trItemType.trSPEEDPOST;
            WorldPosition = worldPosition;
            CreateRPData(position);

            IsMilePost = false;
            IsLimit = true;
            IsFreight = IsPassenger = true;
            IsWarning = isWarning;

            if (!isStart) { IsLimit = true; IsResume = true; }//end zone
            float speed = routeFile.TempRestrictedSpeed;
            if (speed < 0) speed = ORTS.Common.MpS.FromKpH(25); //todo. Value is not used. Should it be used below instead of TempRestrictedSpeed? And if so, is the +0.01 then still needed?
            if (routeFile.MilepostUnitsMetric == true)
            {
                this.IsMPH = false;
                SpeedInd = (int)(ORTS.Common.MpS.ToKpH(routeFile.TempRestrictedSpeed) + 0.1f); 
            }
            else
            {
                this.IsMPH = true;
                SpeedInd = (int)(ORTS.Common.MpS.ToMpH(routeFile.TempRestrictedSpeed) + 0.1f);
            }

            Angle = 0;
        }

        /// <summary>
        /// Create the R P data from a position
        /// </summary>
        /// <param name="position">Position of the speedpost</param>
        private void CreateRPData(Position position)
        {
            X = PX = position.X;
            Z = PZ = position.Z;
            Y = position.Y;
            TileX = TilePX = position.TileX;
            TileZ = TilePZ = position.TileZ;
        }

    }

    /// <summary>
    /// Represents a region where a sound can be played.
    /// </summary>
    public class SoundRegionItem : TrItem
    {
        /// <summary>Sound region data 1</summary>
        public uint SRData1 { get; set; }
        /// <summary>Sound region data 2</summary>
        public uint SRData2 { get; set; }
        /// <summary>Sound region data 3</summary>
        public float SRData3 { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public SoundRegionItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSOUNDREGION;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("tritemsrdata", ()=>{
                    stf.MustMatch("(");
                    SRData1 = stf.ReadUInt(null);
                    SRData2 = stf.ReadUInt(null);
                    SRData3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    /// <summary>
    /// represent an empty item (which probably should only happen for badly defined routes?)
    /// </summary>
    public class EmptyItem : TrItem
    {
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public EmptyItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trEMPTY;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
            });
        }
    }

    /// <summary>
    /// Representa a level Crossing item (so track crossing road)
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification="Keeping identifier consistent to use in MSTS")]
    public class LevelCrItem : TrItem
    {
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public LevelCrItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trXING;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),
            });
        }
    }

    /// <summary>
    /// Represents either start or end of a siding.
    /// </summary>
    public class SidingItem : TrItem
    {
        /// <summary>Flags 1 for a siding ???</summary>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", Justification = "It simply describesr real flags in MSTS file")]
        public string Flags1 { get; set; }
        /// <summary>Flags 2 for a siding, probably the index of the other end of the siding.</summary>
        public uint LinkedSidingId { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public SidingItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSIDING;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("sidingname", ()=>{ ItemName = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("sidingtritemdata", ()=> {
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    LinkedSidingId = stf.ReadUInt(null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
    
    /// <summary>
    /// Represents either start or end of a platform (a place where trains can stop).
    /// </summary>
    public class PlatformItem : TrItem
    {

        /// <summary>Name of the station where the platform is</summary>
        public string Station { get; set; }
        /// <summary>Flags 1 for a platform ???</summary>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", Justification = "It simply describesr real flags in MSTS file")]
        public string Flags1 { get; set; }
        /// <summary>Minimum waiting time at the platform</summary>
        public uint PlatformMinWaitingTime { get; set; }
        /// <summary>Number of passengers waiting at the platform</summary>
        public uint PlatformNumPassengersWaiting { get; set; }
        /// <summary>TrItem Id of the other end of the platform</summary>
        public uint LinkedPlatformItemId { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public PlatformItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trPLATFORM;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("platformname", ()=>{ ItemName = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("station", ()=>{ Station = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("platformminwaitingtime", ()=>{ PlatformMinWaitingTime = stf.ReadUIntBlock(null); }),
                new STFReader.TokenProcessor("platformnumpassengerswaiting", ()=>{ PlatformNumPassengersWaiting = stf.ReadUIntBlock(null); }),
                new STFReader.TokenProcessor("platformtritemdata", ()=>{
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    LinkedPlatformItemId = stf.ReadUInt(null);
                    stf.SkipRestOfBlock();
                }),
            });
        }

        /// <summary>
        /// Constructor to create Platform Item out of Siding Item
        /// </summary>
        /// <param name="thisSiding">The siding to use for a platform creation</param>
        public PlatformItem(SidingItem thisSiding)
        {
            TrItemId = thisSiding.TrItemId;
            SData1 = thisSiding.SData1;
            SData2 = thisSiding.SData2;
            ItemName = thisSiding.ItemName;
            Flags1 = thisSiding.Flags1;
            LinkedPlatformItemId = thisSiding.LinkedSidingId;
            Station = String.Copy(ItemName);
        }
    }

    /// <summary>
    /// Represends a hazard, a place where something more or less dangerous happens
    /// </summary>
    public class HazzardItem : TrItem
    {
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public HazzardItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trHAZZARD;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

            });
        }
    }

    /// <summary>
    /// Represents a pickup, a place to pickup fuel, water, ...
    /// </summary>
    public class PickupItem : TrItem
    {
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        public PickupItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trPICKUP;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

            });
        }
    }

    #region CrossReference to TrackCircuitSection
    /// <summary>
    /// To make it possible for a MSTS (vector) TrackNode to have information about the TrackCircuitSections that
    /// represent that TrackNode, this class defines the basic information of a single of these TrackCircuitSections.
    /// </summary>
    public class TrackCircuitSectionXref
    {
        /// <summary>full length</summary>
        public float Length { get; set; }
        /// <summary>Offset length in orig track section, for either forward or backward direction</summary>
        public float[] OffsetLength;
        /// <summary>index of TrackCircuitSection</summary>
        public int Index { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TrackCircuitSectionXref()
        {
            //Offset indicates length from end of original tracknode, Index 0 is forward, index 1 is backward wrt original tracknode direction.
            OffsetLength = new float[2];
        }

        /// <summary>
        /// Constructor and setting reference, length and offset length from section
        /// </summary>
        /// <param name="sectionIndex"></param>
        /// <param name="sectionLength"></param>
        public TrackCircuitSectionXref(int sectionIndex, float sectionLength, float[] sectionOffsetLength)
        {
            Index = sectionIndex;
            Length = sectionLength;
            OffsetLength = new float[2];
            OffsetLength[0] = sectionOffsetLength[0];
            OffsetLength[1] = sectionOffsetLength[1];
        }
    }

    /// <summary>
    /// Class to make it possible for a MSTS (vector) TrackNode to have information about the TrackCircuitSections that
    /// represent that TrackNode.
    /// </summary>
    public class TrackCircuitXRefList : List<TrackCircuitSectionXref>
    {
        /// <summary>
        /// The tracksections form together a representation of a vector node. Once you give a direction along that vector
        /// and the offset from the start, get the index of the TrackCircuitSectionXref at that location
        /// </summary>
        /// <param name="offset">Offset along the vector node where we want to find the tracksection</param>
        /// <param name="direction">Direction where we start measuring along the vector node</param>
        /// <returns>Index in the current list of crossreferences</returns>
        public int GetXRefIndex(float offset, int direction)
        {
            if (direction == 0)
            {   // search forward, start at the second one (first one should have offsetlength zero
                for (int TC = 1; TC < this.Count; TC++)
                {
                    if (this[TC].OffsetLength[direction] > offset)
                    {
                        return (TC - 1);
                    }
                }

                // not yet found, try the last one
                TrackCircuitSectionXref thisReference = this[this.Count - 1];
                if (offset <= (thisReference.OffsetLength[direction] + thisReference.Length))
                {
                    return (this.Count - 1);
                }

                //really not found, return the first one
                return (0);
            }
            else
            {   // search backward, start at last -1 (because last should end at vector node end anyway
                for (int TC = this.Count - 2; TC >= 0; TC--)
                {
                    if (this[TC].OffsetLength[direction] > offset)
                    {
                        return (TC + 1);
                    }
                }

                //not yet found, try the first one.
                TrackCircuitSectionXref thisReference = this[0];
                if (offset <= (thisReference.OffsetLength[direction] + thisReference.Length))
                {
                    return (0);
                }

                //really not found, return the last one
                return (this.Count - 1);
            }
        }

        /// <summary>
        /// The tracksections form together a representation of a vector node. Once you give a direction along that vector
        /// and the offset from the start, get the index of the TrackCircuitSection at that location
        /// </summary>
        /// <param name="offset">Offset along the vector node where we want to find the tracksection</param>
        /// <param name="direction">Direction where we start measuring along the vector node</param>
        /// <returns>Index of the section that is at the wanted location</returns>
        public int GetSectionIndex(float offset, int direction)
        {
            int XRefIndex = GetXRefIndex(offset, direction);

            if (XRefIndex >= 0)
            {
                TrackCircuitSectionXref thisReference = this[XRefIndex];
                return (thisReference.Index);
            }
            else
            {
                return (-1);
            }
        }
    } // class TrackCircuitXRefList
    #endregion
}
