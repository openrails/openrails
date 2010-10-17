/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;

namespace MSTS
{
    /// <summary>
    /// Summary description for TDBFile.
    /// </summary>
    /// 

 
    public class TDBFile
    {
        public TDBFile(string filenamewithpath)
        {
            using(STFReader f = new STFReader(filenamewithpath))
            {
                string token = f.ReadItem();
                while (token != "") // EOF
                {
                    if (token == ")") throw new STFException(f, "Unexpected )");
                    else if (token == "(") f.SkipBlock();
                    else if (0 == String.Compare(token, "TrackDB", true)) TrackDB = new TrackDB(f);
                    else f.SkipBlock();
                    token = f.ReadItem();
                }
            }
        }

        /// <summary>
        /// Provide a link to the TrJunctionNode for the switch track with 
        /// the specified UiD on the specified tile.
        /// 
        /// Called by switch track shapes to determine the correct position of the points.
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileZ"></param>
        /// <param name="UiD"></param>
        /// <returns></returns>
        public TrJunctionNode GetTrJunctionNode(int tileX, int tileZ, int UiD )
        {
            foreach( TrackNode tn in TrackDB.TrackNodes )
                if (tn != null && tn.TrJunctionNode != null)
                {
                    if ( tileX == tn.UiD.WorldTileX
                        && tileZ == tn.UiD.WorldTileZ
                        && UiD == tn.UiD.WorldID )
                        return tn.TrJunctionNode;
                }
            throw new InvalidDataException("TDB Error, could not find junction.");
        }

        public TrackDB TrackDB;  // Warning, the first TDB entry is always null
    }



    public class TrackDB
    {
        public TrackDB(STFReader f)
        {
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrackNodes", true))
                {
                    f.MustMatch("(");
                    int count = f.ReadInt();
                    TrackNodes = new TrackNode[count + 1];
                    count = 1;
                    token = f.ReadItem();
                    while (token != ")")
                    {
                        if (token == "") throw new STFException(f, "Missing )");
                        else if (0 == String.Compare(token, "TrackNode", true))
                        {
                            TrackNodes[count] = new TrackNode(f, count);
                            ++count;
                        }
                        else f.SkipBlock();
                        token = f.ReadItem();
                    }
                }
                else if (0 == String.Compare(token, "TrItemTable", true))
                {
                    f.MustMatch("(");
                    int count = f.ReadInt();
                    TrItemTable = new TrItem[count];
                    count = 0;
                    token = f.ReadItem();
                    while (token != ")")
                    {
                        if (token == "") throw new STFException(f, "Missing )");
                        else if (0 == String.Compare(token, "CrossoverItem", true))
                        {
                            TrItemTable[count]=new CrossoverItem(f,count);
                        }
                        else if (0 == String.Compare(token, "SignalItem", true))
                        {
                            TrItemTable[count] = new SignalItem(f, count);
                        }
                        else if (0 == String.Compare(token, "SpeedPostItem", true))
                        {
                            TrItemTable[count] = new SpeedPostItem(f, count);
                        }
                        else if (0 == String.Compare(token, "PlatformItem", true))
                        {
                            TrItemTable[count] = new PlatformItem(f, count);
                        }
                        else if (0 == String.Compare(token, "SoundRegionItem", true))
                        {
                            TrItemTable[count] = new SoundRegionItem(f, count);
                        }
                        else if (0 == String.Compare(token, "EmptyItem", true))
                        {
                            TrItemTable[count] = new EmptyItem(f, count);
                        }
                        else if (0 == String.Compare(token, "LevelCrItem", true))
                        {
                            TrItemTable[count] = new LevelCrItem(f, count);
                        }
                        else if (0 == String.Compare(token, "SidingItem", true))
                        {
                            TrItemTable[count] = new CrossoverItem(f, count);
                        }
                        else if (0 == String.Compare(token, "HazzardItem", true))
                        {
                            TrItemTable[count] = new HazzardItem(f, count);
                        }
                        else if (0 == String.Compare(token, "PickupItem", true))
                        {
                            TrItemTable[count] = new PickupItem(f, count);
                        }
                        else f.SkipBlock();
                        ++count;
                        token = f.ReadItem();
                    }
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        public TrackNode[] TrackNodes;
        public TrItem[] TrItemTable;


        public int TrackNodesIndexOf(TrackNode targetTN)
        {
            for (int i = 0; i < TrackNodes.Length; ++i)
                if (TrackNodes[i] == targetTN)
                    return i;
            throw new InvalidOperationException("Program Bug: Can't Find Track Node");
        }
    }


    public class TrackNode
    {
        public TrackNode(STFReader f, int count)
        {
            f.MustMatch("(");
            uint index = f.ReadUInt();
            Debug.Assert(count == index, "TrackNode Index Mismatch");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrJunctionNode", true)) TrJunctionNode = new TrJunctionNode(f);
                else if (0 == String.Compare(token, "TrVectorNode", true)) TrVectorNode = new TrVectorNode(f);
                else if (0 == String.Compare(token, "UiD", true)) UiD = new UiD(f);
                else if (0 == String.Compare(token, "TrEndNode", true)) TrEndNode = new TrEndNode(f);
                else if (0 == String.Compare(token, "TrPins", true))
                {
                    f.MustMatch("(");
                    Inpins = f.ReadUInt();
                    Outpins = f.ReadUInt();
                    TrPins = new TrPin[Inpins + Outpins];
                    for (int i = 0; i < Inpins + Outpins; ++i)
                    {
                        f.MustMatch("TrPin");
                        TrPins[i] = new TrPin(f);
                    }
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
            // TODO We assume there is only 2 outputs to each junction
            if (TrVectorNode != null && TrPins.Length != 2)
                Trace.TraceError("TDB DEBUG TVN={0} has {1} pins.", UiD, TrPins.Length);
        }
        public TrJunctionNode TrJunctionNode = null;
        public TrVectorNode TrVectorNode = null;
        public TrEndNode TrEndNode = null;
        public TrPin[] TrPins;
        public UiD UiD = null;  // only provided for TrJunctionNode and TrEndNode type of TrackNodes
        public uint Inpins;
        public uint Outpins;
    }

    [DebuggerDisplay("\\{MSTS.TrPin\\} Link={Link}, Dir={Direction}")]
    public class TrPin
    {
        public TrPin(STFReader f)
        {
            f.MustMatch("(");
            Link = f.ReadInt();
            Direction = f.ReadInt();
            f.MustMatch(")");
        }
        public int Link;
        public int Direction;
    }

    [DebuggerDisplay("\\{MSTS.UiD\\} ID={WorldID}, TileX={TileX}, TileZ={TileZ}, X={X}, Y={Y}, Z={Z}, AX={AX}, AY={AY}, AZ={AZ}, WorldX={WorldTileX}, WorldZ={WorldTileZ}")]
    public class UiD
    {
        public int TileX, TileZ;   // location of the junction 
        public float X, Y, Z;
        public float AX, AY, AZ;

        public int WorldTileX, WorldTileZ, WorldID;  // cross reference to the entry in the world file

        public UiD(STFReader f)
        {
            // UiD ( -11283 14482 5 0 -11283 14482 -445.573 239.861 186.111 0 -3.04199 0 )

            f.MustMatch("(");
            WorldTileX = f.ReadInt();            // -11283
            WorldTileZ = f.ReadInt();            // 14482
            WorldID = f.ReadInt();              // 5
            f.ReadInt();                        // 0
            TileX = f.ReadInt();            // -11283
            TileZ = f.ReadInt();            // 14482
            X = f.ReadFloat();         // -445.573
            Y = f.ReadFloat();         // 239.861
            Z = f.ReadFloat();         // 186.111
            AX = f.ReadFloat();         // 0
            AY = f.ReadFloat();         // -3.04199
            AZ = f.ReadFloat();         // 0
            f.MustMatch(")");
        }

    }

    [DebuggerDisplay("\\{MSTS.TrJunctionNode\\} SelectedRoute={SelectedRoute}, ShapeIndex={ShapeIndex}")]
    public class TrJunctionNode
    {
        public int SelectedRoute = 0;

        public TrJunctionNode(STFReader f)
        {
            f.MustMatch("(");
            f.ReadItem();
            ShapeIndex = f.ReadUInt();
            f.ReadItem();
            f.MustMatch(")");
        }
        public uint ShapeIndex;
    }

    public class TrVectorNode
    {
        public TrVectorNode(STFReader f)
        {
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrVectorSections", true))
                {
                    f.MustMatch("(");
                    int count = f.ReadInt();
                    TrVectorSections = new TrVectorSection[count];
                    for (int i = 0; i < count; ++i)
                    {
                        TrVectorSections[i] = new TrVectorSection(f);
                    }
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRefs", true)) ItemRefs(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        public TrVectorSection[] TrVectorSections;
        public int[] TrItemRefs;
        public int noItemRefs =0;

        public int TrVectorSectionsIndexOf(TrVectorSection targetTVS)
        {
            for (int i = 0; i < TrVectorSections.Length; ++i)
                if (TrVectorSections[i] == targetTVS)
                    return i;
            throw new InvalidOperationException("Program Bug: Can't Find TVS");
        }

        // Build a list of track items associated with this node
        private void ItemRefs(STFReader f)
        {
            int count = 0;

            f.MustMatch("(");
            noItemRefs=f.ReadInt();
            TrItemRefs = new int[noItemRefs];
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemRef", true))
                {
                    if (count < noItemRefs)
                    {
                        TrItemRefs[count] = f.ReadIntBlock();
                        count++;
                    }
                    else
                    {
                        throw new STFException(f, "TrItemRef Count Mismatch");
                    }
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
            if (count != noItemRefs) throw new STFException(f, "TrItemRef Count Mismatch");
        }
    }

    public class TrVectorSection
    {
        public TrVectorSection(STFReader f)
        {
            SectionIndex = f.ReadUInt();
            ShapeIndex = f.ReadUInt();
            f.ReadItem(); // worldfilenamex
            f.ReadItem(); // worldfilenamez
            WorldFileUiD = f.ReadUInt(); // UID in worldfile
            flag1 = f.ReadInt(); // 0
            flag2 = f.ReadInt(); // 1
            f.ReadItem(); // 00 
            TileX = f.ReadInt();
            TileZ = f.ReadInt();
            X = f.ReadFloat();
            Y = f.ReadFloat();
            Z = f.ReadFloat();
            AX = f.ReadFloat();
            AY = f.ReadFloat();
            AZ = f.ReadFloat();
        }
        public int flag1;   // usually 0, - may point to the connecting pin entry in a junction
        public int flag2;  // usually 1, but set to 0 when curve track is flipped around
        // I have also seen 2's in both locations
        public uint SectionIndex;
        public uint ShapeIndex;
        public int TileX;
        public int TileZ;
        public float X, Y, Z;
        public float AX, AY, AZ;
        public uint WorldFileUiD;
    }


    public class TrEndNode
    {
        public TrEndNode(STFReader f)
        {
            f.MustMatch("(");
            f.ReadItem();
            f.MustMatch(")");
        }
    }

    public class TrItem
    {
        public enum trItemType
        {
            trEMPTY,
            trCROSSOVER,
            trSIGNAL,
            trSPEEDPOST,
            trPLATFORM,
            trSOUNDREGION,
            trXING,
            trSIDING,
            trHAZZARD,
            trPICKUP
        }
        public trItemType ItemType = trItemType.trEMPTY;
        public uint TrItemId;
        public int TileX;
        public int TileZ;
        public float X, Y, Z;   //  Location within world tile. Track node not shape
        public int TilePX;
        public int TilePZ;      // Appears to be copy of SData but X,Z only.
        public float PX, PZ;   
        public float SData1;
        public string SData2;
        //public TrItem()
        //{

        //}
        protected void TrItemRData(STFReader f)
        {
            f.MustMatch("(");
            X = f.ReadFloat();
            Y = f.ReadFloat();
            Z = f.ReadFloat();
            TileX = f.ReadInt();
            TileZ = f.ReadInt();
            f.MustMatch(")");
        }

        protected void TrItemPData(STFReader f)
        {
            f.MustMatch("(");
            PX = f.ReadFloat();
            PZ = f.ReadFloat();
            TilePX = f.ReadInt();
            TilePZ = f.ReadInt();
            f.MustMatch(")");
        }

        protected void TrItemSData(STFReader f)
        {
            f.MustMatch("(");
            SData1 = f.ReadFloat();
            SData2 = f.ReadItem();
            f.MustMatch(")");
        }
    }

    public class CrossoverItem:TrItem
    {
        uint TrackNode, CID1;
        public CrossoverItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trCROSSOVER;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "CrossoverItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "CrossoverTrItemData", true)) CrossoverTrItemData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        private void CrossoverTrItemData(STFReader f)
        {
            f.MustMatch("(");
            TrackNode = f.ReadUInt();
            CID1 = f.ReadUInt();
            f.MustMatch(")");
        }
    }

    public class SignalItem : TrItem
    {
        public struct strTrSignalDir
        {
            public uint TrackNode;                  // Index to the junction track node
            public uint sd1, linkLRPath, sd3;              // Used with junction signals (appears to be either 1 or 0
        }

        public string Flags1;                 // Set to  00000001 if junction link set 
        public uint Direction;                // 0 or 1 depending on which way signal is facing
        public int sigObj = -1;               // index to Sigal Object Table
        public float SigData1;
        public string SignalType;
        public uint noSigDirs=0;              // Number of junction links
        public strTrSignalDir[] TrSignalDirs;
        
        public int revDir
        {
            get {return Direction==0?1:0;}
        }

        public SignalItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trSIGNAL;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SignalItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "TrSignalType", true)) this.TrSignalType(f);
                else if (0 == String.Compare(token, "TrSignalDirs", true)) this.TrSigDirs(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        private void TrSignalType(STFReader f)
        {
            f.MustMatch("(");
            Flags1 = f.ReadItem();
            Direction = f.ReadUInt();
            SigData1 = f.ReadFloat();
            SignalType = f.ReadItem();
            // To do get index to Sigtypes table corresponding to this sigmal
            f.MustMatch(")");
        }
        private void TrSigDirs(STFReader f)
        {
            f.MustMatch("(");
            this.noSigDirs = f.ReadUInt();
            TrSignalDirs = new strTrSignalDir[noSigDirs];
            int count=0;
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrSignalDir", true)) 
                {
                    if(count<noSigDirs)
                    {
                        TrSignalDirs[count]=new strTrSignalDir();
                        f.MustMatch("(");
                        TrSignalDirs[count].TrackNode=f.ReadUInt();
                        TrSignalDirs[count].sd1=f.ReadUInt();
                        TrSignalDirs[count].linkLRPath = f.ReadUInt(); 
                        TrSignalDirs[count].sd3=f.ReadUInt();
                        f.MustMatch(")");
                        count++;
                    }
                    else
                    {
                        throw new STFException(f, "TrSignalDirs count mismatch");
                    }
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
            if(count!=noSigDirs)throw new STFException(f, "TrSignalDirs count mismatch");
        }
    }

    public class SpeedPostItem : TrItem
    {
        uint Flags;
        float SpeedInd;      // Or distance if mile post.
        float SID1;
        public SpeedPostItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trSPEEDPOST;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SpeedPostItem Index Mismatch");
                    f.MustMatch(")");
                }
                //else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                //else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                //else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                //else if (0 == String.Compare(token, "SpeedpostTrItemData", true)) SpeedpostTrItemData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        private void SpeedpostTrItemData(STFReader f)
        {
            this.ItemType = trItemType.trSPEEDPOST;
            f.MustMatch("(");
            Flags = f.ReadUInt();
            //
            //  The number of parameters depends on the flags seeting
            //  To do: Check flags seetings and parse accordingly.
            //
            SpeedInd = f.ReadFloat();
            SID1 = f.ReadFloat();
            f.MustMatch(")");
        }
    }

    public class PlatformItem : TrItem
    {
        public string PlatformName, Station;
        public string Flags1;
        public uint PlatformMinWaitingTime, PlatformNumPassengersWaiting;
        public uint Flags2;

        public PlatformItem(STFReader f, int count)
        {
            this.ItemType=trItemType.trPLATFORM;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "PlatformItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "PlatformName", true))
                {
                    f.MustMatch("(");
                    PlatformName = f.ReadItem();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "Station", true))
                {
                    f.MustMatch("(");
                    Station = f.ReadItem();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformMinWaitingTime", true))
                {
                    f.MustMatch("(");
                    PlatformMinWaitingTime = f.ReadUInt();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformNumPassengersWaiting", true))
                {
                    f.MustMatch("(");
                    PlatformNumPassengersWaiting = f.ReadUInt();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformTrItemData", true)) PlatformTrItemData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        private void PlatformTrItemData(STFReader f)
        {
            f.MustMatch("(");
            Flags1 = f.ReadItem();
            Flags2 = f.ReadUInt();
            f.MustMatch(")");
        }
    }

    public class SoundRegionItem : TrItem
    {
        public uint SRData1, SRData2;
        public float SRData3;
        public SoundRegionItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trSOUNDREGION;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SoundRegionItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) base.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
        private void TrItemSRData(STFReader f)
        {
            f.MustMatch("(");
            SRData1 = f.ReadUInt();
            SRData2 = f.ReadUInt();
            SRData3 = f.ReadFloat();
            f.MustMatch(")");
        }
    }

    public class EmptyItem : TrItem
    {
        public EmptyItem(STFReader f, int count)
        {
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "EmptyItem Index Mismatch");
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
    }

    public class LevelCrItem : TrItem
    {
        public LevelCrItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trXING;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "LevelCrItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) base.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
    }

    public class SidingItem : TrItem
    {
        public string SidingName;
        public string Flags1;
        public uint Flags2;

        public SidingItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trSIDING;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SidingItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "SidingTrItemData", true)) SidingTrItemData(f);
                else if (0 == String.Compare(token, "SidingName", true))
                {
                    f.MustMatch("(");
                    SidingName = f.ReadItem();
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }

        private void SidingTrItemData(STFReader f)
        {
            f.MustMatch("(");
            Flags1 = f.ReadItem();
            Flags2 = f.ReadUInt();
            f.MustMatch(")");
        }
    }

    public class HazzardItem : TrItem
    {
        public HazzardItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trHAZZARD;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "HazzardItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) base.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
    }

    public class PickupItem : TrItem
    {
        public PickupItem(STFReader f, int count)
        {
            this.ItemType = trItemType.trPICKUP;
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.MustMatch("(");
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "PickupItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) base.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) base.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) base.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadItem();
            }
        }
    }

}
