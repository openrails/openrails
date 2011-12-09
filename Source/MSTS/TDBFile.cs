/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;
using ORTS.Interlocking;

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
            using (STFReader stf = new STFReader(filenamewithpath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackdb", ()=>{ TrackDB = new TrackDB(stf); }),
                });
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
        public TrJunctionNode GetTrJunctionNode(int tileX, int tileZ, int UiD)
        {
            foreach (TrackNode tn in TrackDB.TrackNodes)
                if (tn != null && tn.TrJunctionNode != null)
                    if (tileX == tn.UiD.WorldTileX && tileZ == tn.UiD.WorldTileZ && UiD == tn.UiD.WorldID)
                        return tn.TrJunctionNode;

            Trace.TraceWarning("Track node in tile {0},{1} with UiD {2} could not be found in TDB.", tileX, tileZ, UiD);
            return null;
        }

        public TrackDB TrackDB;  // Warning, the first TDB entry is always null
    }



    public class TrackDB
    {
        public TrackDB(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tracknodes", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(STFReader.UNITS.None, null);
                    TrackNodes = new TrackNode[count + 1];
                    int idx = 1;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tracknode", ()=>{ TrackNodes[idx] = new TrackNode(stf, idx); ++idx; }),
                    });
                }),
                new STFReader.TokenProcessor("tritemtable", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(STFReader.UNITS.None, null);
                    TrItemTable = new TrItem[count];
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
        public TrackNode(STFReader stf, int idx)
        {
            stf.MustMatch("(");
            uint index = stf.ReadUInt(STFReader.UNITS.None, null);
            Debug.Assert(idx == index, "TrackNode Index Mismatch");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = new UiD(stf); }),
                new STFReader.TokenProcessor("trjunctionnode", ()=>{ TrJunctionNode = new TrJunctionNode(stf); }),
                new STFReader.TokenProcessor("trvectornode", ()=>{ TrVectorNode = new TrVectorNode(stf); }),
                new STFReader.TokenProcessor("trendnode", ()=>{ TrEndNode = true; stf.SkipBlock(); }),
                new STFReader.TokenProcessor("trpins", ()=>{
                    stf.MustMatch("(");
                    Inpins = stf.ReadUInt(STFReader.UNITS.None, null);
                    Outpins = stf.ReadUInt(STFReader.UNITS.None, null);
                    TrPins = new TrPin[Inpins + Outpins];
                    for (int i = 0; i < Inpins + Outpins; ++i)
                    {
                        stf.MustMatch("TrPin");
                        TrPins[i] = new TrPin(stf);
                    }
                    stf.SkipRestOfBlock();
                }),
            });
            // TODO We assume there is only 2 outputs to each junction
            if (TrVectorNode != null && TrPins.Length != 2)
                Trace.TraceWarning("TDB DEBUG TVN={0} has {1} pins.", UiD, TrPins.Length);
        }
        public TrJunctionNode TrJunctionNode;
        public TrVectorNode TrVectorNode;
        
       /// <summary>
       /// True when this TrackNode has nothing else connected to it (that is, it is
       /// a buffer end or an unfinished track) and trains cannot proceed beyond here.
       /// </summary>
       public bool TrEndNode;

        public TrPin[] TrPins;
        public UiD UiD;  // only provided for TrJunctionNode and TrEndNode type of TrackNodes
        public uint Inpins;
        public uint Outpins;


        public InterlockingTrack InterlockingTrack { get; set; }
    
    }

    [DebuggerDisplay("\\{MSTS.TrPin\\} Link={Link}, Dir={Direction}")]
    public class TrPin
    {
        public TrPin(STFReader stf)
        {
            stf.MustMatch("(");
            Link = stf.ReadInt(STFReader.UNITS.None, null);
            Direction = stf.ReadInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
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

        public UiD(STFReader stf)
        {
            stf.MustMatch("(");
            WorldTileX = stf.ReadInt(STFReader.UNITS.None, null);
            WorldTileZ = stf.ReadInt(STFReader.UNITS.None, null);
            WorldID = stf.ReadInt(STFReader.UNITS.None, null);
            stf.ReadInt(STFReader.UNITS.None, null);
            TileX = stf.ReadInt(STFReader.UNITS.None, null);
            TileZ = stf.ReadInt(STFReader.UNITS.None, null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            AX = stf.ReadFloat(STFReader.UNITS.None, null);
            AY = stf.ReadFloat(STFReader.UNITS.None, null);
            AZ = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }

    }

    [DebuggerDisplay("\\{MSTS.TrJunctionNode\\} SelectedRoute={SelectedRoute}, ShapeIndex={ShapeIndex}")]
    public class TrJunctionNode
    {
        public int SelectedRoute = 0;

        public TrJunctionNode(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ReadString();
            ShapeIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
        public uint ShapeIndex;

		public double angle = -1;
		public bool AngleComputed = false; //the angle has been set through section file

		public double GetAngle(TSectionDatFile TSectionDat) //get the angle from sections
		{
			if (AngleComputed == false)
			{
				AngleComputed = true;
				try //so many things can be in conflict for trackshapes, tracksections etc.
				{
					SectionIdx[] SectionIdxs = TSectionDat.TrackShapes.Get(ShapeIndex).SectionIdxs;

					foreach (SectionIdx id in SectionIdxs)
					{
						uint[] sections = id.TrackSections;

						for (int i = 0; i < sections.Length; i++)
						{
							uint sid = id.TrackSections[i];
							TrackSection section = TSectionDat.TrackSections[sid];

							if (section.SectionCurve != null)
							{
								angle = Math.Abs(section.SectionCurve.Angle);
								break;
							}
						}
					}
				}
				catch (Exception e) { } 
			}
			return angle;
		}
    }

    public class TrVectorNode
    {
        public TrVectorNode(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("trvectorsections", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(STFReader.UNITS.None, null);
                    TrVectorSections = new TrVectorSection[count];
                    for (int i = 0; i < count; ++i)
                        TrVectorSections[i] = new TrVectorSection(stf);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("tritemrefs", ()=>{
                    stf.MustMatch("(");
                    noItemRefs = stf.ReadInt(STFReader.UNITS.None, null);
                    TrItemRefs = new int[noItemRefs];
                    int refidx = 0;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tritemref", ()=>{
                            if (refidx < noItemRefs)
                                TrItemRefs[refidx++] = stf.ReadIntBlock(STFReader.UNITS.None, null);
                            else
                                STFException.TraceWarning(stf, "TrItemRef Count Mismatch");
                        }),
                    });
                    if(refidx != noItemRefs)
                        STFException.TraceWarning(stf, "TrItemRef Count Mismatch");
                }),
            });
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

    }

    public class TrVectorSection
    {
        public TrVectorSection(STFReader stf)
        {
            SectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            ShapeIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.ReadString(); // worldfilenamex
            stf.ReadString(); // worldfilenamez
            WorldFileUiD = stf.ReadUInt(STFReader.UNITS.None, null); // UID in worldfile
            flag1 = stf.ReadInt(STFReader.UNITS.None, null); // 0
            flag2 = stf.ReadInt(STFReader.UNITS.None, null); // 1
            stf.ReadString(); // 00 
            TileX = stf.ReadInt(STFReader.UNITS.None, null);
            TileZ = stf.ReadInt(STFReader.UNITS.None, null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            AX = stf.ReadFloat(STFReader.UNITS.None, null);
            AY = stf.ReadFloat(STFReader.UNITS.None, null);
            AZ = stf.ReadFloat(STFReader.UNITS.None, null);
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


    public class TrItem
    {
        public string ItemName;   // Used for the label shown by F6
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
            trPICKUP,
			trCarSpawner // added for road traffic spawner, used in the future
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
        
        protected void TrItemID(STFReader stf, int idx)
        {
            stf.MustMatch("(");
            TrItemId = stf.ReadUInt(STFReader.UNITS.None, null);
            Debug.Assert(idx == TrItemId, "Index Mismatch");
            stf.SkipRestOfBlock();
        }
        protected void TrItemRData(STFReader stf)
        {
            stf.MustMatch("(");
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            TileX = stf.ReadInt(STFReader.UNITS.None, null);
            TileZ = stf.ReadInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }

        protected void TrItemPData(STFReader stf)
        {
            stf.MustMatch("(");
            PX = stf.ReadFloat(STFReader.UNITS.None, null);
            PZ = stf.ReadFloat(STFReader.UNITS.None, null);
            TilePX = stf.ReadInt(STFReader.UNITS.None, null);
            TilePZ = stf.ReadInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }

        protected void TrItemSData(STFReader stf)
        {
            stf.MustMatch("(");
            SData1 = stf.ReadFloat(STFReader.UNITS.None, null);
            SData2 = stf.ReadString();
            stf.SkipRestOfBlock();
        }
    } // TrItem

    public class CrossoverItem : TrItem
    {
        uint TrackNode, CID1;
        public CrossoverItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trCROSSOVER;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("crossovertritemdata", ()=>{
                    stf.MustMatch("(");
                    TrackNode = stf.ReadUInt(STFReader.UNITS.None, null);
                    CID1 = stf.ReadUInt(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
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

        public SignalItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSIGNAL;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("trsignaltype", ()=>{
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    Direction = stf.ReadUInt(STFReader.UNITS.None, null);
                    SigData1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    SignalType = stf.ReadString().ToLowerInvariant();
                    // To do get index to Sigtypes table corresponding to this sigmal
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("trsignaldirs", ()=>{
                    stf.MustMatch("(");
                    noSigDirs = stf.ReadUInt(STFReader.UNITS.None, null);
                    TrSignalDirs = new strTrSignalDir[noSigDirs];
                    int sigidx = 0;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trsignaldir", ()=>{
                            if(sigidx<noSigDirs)
                            {
                                TrSignalDirs[sigidx]=new strTrSignalDir();
                                stf.MustMatch("(");
                                TrSignalDirs[sigidx].TrackNode = stf.ReadUInt(STFReader.UNITS.None, null);
                                TrSignalDirs[sigidx].sd1 = stf.ReadUInt(STFReader.UNITS.None, null);
                                TrSignalDirs[sigidx].linkLRPath = stf.ReadUInt(STFReader.UNITS.None, null);
                                TrSignalDirs[sigidx].sd3 = stf.ReadUInt(STFReader.UNITS.None, null);
                                stf.SkipRestOfBlock();
                                sigidx++;
                            }
                        }),
                    });
                    if(sigidx != noSigDirs)  STFException.TraceWarning(stf, "TrSignalDirs count mismatch");
                }),
            });
        }
    }

    public class SpeedPostItem : TrItem
    {
        uint Flags;
        float SpeedInd;      // Or distance if mile post.
        public SpeedPostItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSPEEDPOST;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("speedposttritemdata", ()=>{
                    stf.MustMatch("(");
                    Flags = stf.ReadUInt(STFReader.UNITS.None, null);
                    //  The number of parameters depends on the flags seeting
                    //  To do: Check flags seetings and parse accordingly.
                    SpeedInd = stf.ReadFloat(STFReader.UNITS.Speed, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class SoundRegionItem : TrItem
    {
        public uint SRData1, SRData2;
        public float SRData3;
        public SoundRegionItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSOUNDREGION;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("tritemsrdata", ()=>{
                    stf.MustMatch("(");
                    SRData1 = stf.ReadUInt(STFReader.UNITS.None, null);
                    SRData2 = stf.ReadUInt(STFReader.UNITS.None, null);
                    SRData3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class EmptyItem : TrItem
    {
        public EmptyItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trEMPTY;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
            });
        }
    }

    public class LevelCrItem : TrItem

    {
		public uint Direction;                // 0 or 1 depending on which way signal is facing
		public int revDir
		{
			get { return Direction == 0 ? 1 : 0; }
		}

        public LevelCrItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trXING;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

            });
        }
    }

    public class SidingItem : TrItem
    {
        public string Flags1;
        public uint Flags2;

        public SidingItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trSIDING;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("sidingname", ()=>{ ItemName = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("sidingtritemdata", ()=> {
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    Flags2 = stf.ReadUInt(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
    
    public class PlatformItem : TrItem {
        public string Station;
        public string Flags1;
        public uint PlatformMinWaitingTime, PlatformNumPassengersWaiting;
        public uint LinkedPlatformItemId;

        public PlatformItem(STFReader stf, int idx) {
        ItemType = trItemType.trPLATFORM;
        stf.MustMatch("(");
        stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

                new STFReader.TokenProcessor("platformname", ()=>{ ItemName = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("station", ()=>{ Station = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("platformminwaitingtime", ()=>{ PlatformMinWaitingTime = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("platformnumpassengerswaiting", ()=>{ PlatformNumPassengersWaiting = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("platformtritemdata", ()=>{
                    stf.MustMatch("(");
                    Flags1 = stf.ReadString();
                    LinkedPlatformItemId = stf.ReadUInt(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class HazzardItem : TrItem
    {
        public HazzardItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trHAZZARD;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

            });
        }
    }

    public class PickupItem : TrItem
    {
        public PickupItem(STFReader stf, int idx)
        {
            ItemType = trItemType.trPICKUP;
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ TrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); }),

            });
        }
    }

}
