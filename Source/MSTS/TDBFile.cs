/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Diagnostics;
using MSTSMath;
using Microsoft.Xna.Framework;

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
            STFReader f = new STFReader(filenamewithpath);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") throw (new STFError(f, "Unexpected )"));
                    else if (token == "(") f.SkipBlock();
                    else if (0 == String.Compare(token, "TrackDB", true)) TrackDB = new TrackDB(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
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
            throw new System.Exception("TDB Error, could not find junction.");
        }

        public TrackDB TrackDB;  // Warning, the first TDB entry is always null
    }



    public class TrackDB
    {
        public TrackDB(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrackNodes", true))
                {
                    f.VerifyStartOfBlock();
                    int count = f.ReadInt();
                    TrackNodes = new TrackNode[count + 1];
                    count = 1;
                    token = f.ReadToken();
                    while (token != ")")
                    {
                        if (token == "") throw (new STFError(f, "Missing )"));
                        else if (0 == String.Compare(token, "TrackNode", true))
                        {
                            TrackNodes[count] = new TrackNode(f, count);
                            ++count;
                        }
                        else f.SkipBlock();
                        token = f.ReadToken();
                    }
                }
                else if (0 == String.Compare(token, "TrItemTable", true))
                {
                    f.VerifyStartOfBlock();
                    int count = f.ReadInt();
                    TrItemTable = new TrItem[count];
                    count = 0;
                    token = f.ReadToken();
                    while (token != ")")
                    {
                        if (token == "") throw (new STFError(f, "Missing )"));
                        else if (0 == String.Compare(token, "CrossoverItem", true))
                        {
                            TrItemTable[count]=new CrossoverItem(f,count);
                        }
                        else if (0 == String.Compare(token, "SignalItem", true))
                        {
                            TrItemTable[count] = new SignalItem(f, count);
                        }
                        //else if (0 == String.Compare(token, "SpeedPostItem", true))
                        //{
                        //    TrItemTable[count] = new SpeedPostItem(f, count);
                        //}
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
                        token = f.ReadToken();
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        public TrackNode[] TrackNodes;
        public TrItem[] TrItemTable;


        public int TrackNodesIndexOf(TrackNode targetTN)
        {
            for (int i = 0; i < TrackNodes.Length; ++i)
                if (TrackNodes[i] == targetTN)
                    return i;
            throw new System.Exception("Program Bug: Can't Find Track Node");
        }
    }


    public class TrackNode
    {
        public TrackNode(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            uint index = f.ReadUInt();
            Debug.Assert(count == index, "TrackNode Index Mismatch");
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrJunctionNode", true)) TrJunctionNode = new TrJunctionNode(f);
                else if (0 == String.Compare(token, "TrVectorNode", true)) TrVectorNode = new TrVectorNode(f);
                else if (0 == String.Compare(token, "UiD", true)) UiD = new UiD(f);
                else if (0 == String.Compare(token, "TrEndNode", true)) TrEndNode = new TrEndNode(f);
                else if (0 == String.Compare(token, "TrPins", true))
                {
                    f.VerifyStartOfBlock();
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
                token = f.ReadToken();
            }
            // TODO We assume there is only 2 outputs to each junction
            if (TrVectorNode != null && TrPins.Length != 2)
                Console.Error.WriteLine("TDB DEBUG TVN={0} has {1} pins.", UiD, TrPins.Length);
        }
        public TrJunctionNode TrJunctionNode = null;
        public TrVectorNode TrVectorNode = null;
        public TrEndNode TrEndNode = null;
        public TrPin[] TrPins;
        public UiD UiD = null;  // only provided for TrJunctionNode and TrEndNode type of TrackNodes
        public uint Inpins;
        public uint Outpins;
    }

    public class TrPin
    {
        public TrPin(STFReader f)
        {
            f.VerifyStartOfBlock();
            Link = f.ReadInt();
            Direction = f.ReadInt();
            f.MustMatch(")");
        }
        public int Link;
        public int Direction;
    }

    public class UiD
    {
        public int TileX, TileZ;   // location of the junction 
        public float X, Y, Z;
        public float AX, AY, AZ;

        public int WorldTileX, WorldTileZ, WorldID;  // cross reference to the entry in the world file

        public UiD(STFReader f)
        {
            // UiD ( -11283 14482 5 0 -11283 14482 -445.573 239.861 186.111 0 -3.04199 0 )

            f.VerifyStartOfBlock();
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

    public class TrJunctionNode
    {
        public int SelectedRoute = 0;

        public TrJunctionNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            f.ReadToken();
            ShapeIndex = f.ReadUInt();
            f.ReadToken();
            f.MustMatch(")");
        }
        public uint ShapeIndex;
    }

    public class TrVectorNode
    {
        public TrVectorNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrVectorSections", true))
                {
                    f.VerifyStartOfBlock();
                    int count = f.ReadInt();
                    TrVectorSections = new TrVectorSection[count];
                    for (int i = 0; i < count; ++i)
                    {
                        TrVectorSections[i] = new TrVectorSection(f);
                    }
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        public TrVectorSection[] TrVectorSections;

        public int TrVectorSectionsIndexOf(TrVectorSection targetTVS)
        {
            for (int i = 0; i < TrVectorSections.Length; ++i)
                if (TrVectorSections[i] == targetTVS)
                    return i;
            throw new System.Exception("Program Bug: Can't Find TVS");
        }

    }

    public class TrVectorSection
    {
        public TrVectorSection(STFReader f)
        {
            SectionIndex = f.ReadUInt();
            ShapeIndex = f.ReadUInt();
            f.ReadToken(); // worldfilenamex
            f.ReadToken(); // worldfilenamez
            WorldFileUiD = f.ReadUInt(); // UID in worldfile
            flag1 = f.ReadInt(); // 0
            flag2 = f.ReadInt(); // 1
            f.ReadToken(); // 00 
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
            f.VerifyStartOfBlock();
            f.ReadToken();
            f.MustMatch(")");
        }

    }

    public class TrItem
    {
        public uint TrItemId, TrItemType;
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
            f.VerifyStartOfBlock();
            X = f.ReadFloat();
            Y = f.ReadFloat();
            Z = f.ReadFloat();
            TileX = f.ReadInt();
            TileZ = f.ReadInt();
            f.MustMatch(")");
        }

        protected void TrItemPData(STFReader f)
        {
            f.VerifyStartOfBlock();
            PX = f.ReadFloat();
            PZ = f.ReadFloat();
            TilePX = f.ReadInt();
            TilePZ = f.ReadInt();
            f.MustMatch(")");
        }

        protected void TrItemSData(STFReader f)
        {
            f.VerifyStartOfBlock();
            SData1 = f.ReadFloat();
            SData2 = f.ReadToken();
            f.MustMatch(")");
        }
    }

    public class CrossoverItem:TrItem
    {
        uint TrackNode, CID1;
        public CrossoverItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "CrossoverItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "CrossoverTrItemData", true)) CrossoverTrItemData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        private void CrossoverTrItemData(STFReader f)
        {
            f.VerifyStartOfBlock();
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
            public uint sd1, sd2, sd3;              // Used with junction signals (appears to be either 1 or 0
        }

        public string Flags1;
        public uint Flags2;
        public float SigData1;
        public string SignalType;
        public uint noSigDirs=0;              // Number of junction links
        public strTrSignalDir[] TrSignalDirs;

        public SignalItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SignalItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrSignalType", true)) this.TrSignalType(f);
                else if (0 == String.Compare(token, "TrSignalDirs", true)) this.TrSigDirs(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        private void TrSignalType(STFReader f)
        {
            f.VerifyStartOfBlock();
            Flags1 = f.ReadToken();
            Flags2 = f.ReadUInt();
            SigData1 = f.ReadFloat();
            SignalType = f.ReadString();
            // To do get index to Sigtypes table corresponding to this sigmal
            f.MustMatch(")");
        }
        private void TrSigDirs(STFReader f)
        {
            f.VerifyStartOfBlock();
            this.noSigDirs = f.ReadUInt();
            TrSignalDirs = new strTrSignalDir[noSigDirs];
            int count=0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrSignalDir", true)) 
                {
                    if(count<noSigDirs)
                    {
                        TrSignalDirs[count]=new strTrSignalDir();
                        f.VerifyStartOfBlock();
                        TrSignalDirs[count].TrackNode=f.ReadUInt();
                        TrSignalDirs[count].sd1=f.ReadUInt();
                        TrSignalDirs[count].sd2=f.ReadUInt();
                        TrSignalDirs[count].sd3=f.ReadUInt();
                        f.MustMatch(")");
                        count++;
                    }
                    else
                    {
                        throw (new STFError(f, "TrSignalDirs count mismatch"));
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if(count!=noSigDirs)throw (new STFError(f, "TrSignalDirs count mismatch"));
        }
    }

    public class SpeedPostItem : TrItem
    {
        uint Flags;
        float SpeedInd;      // Or distance if mile post.
        float SID1;
        public SpeedPostItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SpeedPostItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                else if (0 == String.Compare(token, "SpeedpostTrItemData", true)) SpeedpostTrItemData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        private void SpeedpostTrItemData(STFReader f)
        {
            f.VerifyStartOfBlock();
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
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "PlatformItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "PlatformName", true))
                {
                    f.VerifyStartOfBlock();
                    PlatformName = f.ReadString();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "Station", true))
                {
                    f.VerifyStartOfBlock();
                    Station = f.ReadString();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformMinWaitingTime", true))
                {
                    f.VerifyStartOfBlock();
                    PlatformMinWaitingTime = f.ReadUInt();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformNumPassengersWaiting", true))
                {
                    f.VerifyStartOfBlock();
                    PlatformNumPassengersWaiting = f.ReadUInt();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "PlatformTrItemData", true)) PlatformTrItemData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        private void PlatformTrItemData(STFReader f)
        {
            f.VerifyStartOfBlock();
            Flags1 = f.ReadString();
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
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SoundRegionItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        private void TrItemSRData(STFReader f)
        {
            f.VerifyStartOfBlock();
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
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "EmptyItem Index Mismatch");
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
    }

    public class LevelCrItem : TrItem
    {
        public LevelCrItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "LevelCrItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadToken();
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
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "SidingItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "SidingTrItemData", true)) SidingTrItemData(f);
                else if (0 == String.Compare(token, "SidingName", true))
                {
                    f.VerifyStartOfBlock();
                    SidingName = f.ReadString();
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void SidingTrItemData(STFReader f)
        {
            f.VerifyStartOfBlock();
            Flags1 = f.ReadString();
            Flags2 = f.ReadUInt();
            f.MustMatch(")");
        }
    }

    public class HazzardItem : TrItem
    {
        public HazzardItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "HazzardItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
    }

    public class PickupItem : TrItem
    {
        public PickupItem(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrItemID", true))
                {
                    f.VerifyStartOfBlock();
                    this.TrItemId = f.ReadUInt();
                    Debug.Assert(count == this.TrItemId, "PickupItem Index Mismatch");
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "TrItemRData", true)) this.TrItemRData(f);
                else if (0 == String.Compare(token, "TrItemSData", true)) this.TrItemSData(f);
                else if (0 == String.Compare(token, "TrItemPData", true)) this.TrItemPData(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
    }

}
