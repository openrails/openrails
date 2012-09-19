/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///     

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTSMath;


namespace MSTS
{
    public class WFile
    {
        public int TileX, TileZ;
        public Tr_Worldfile Tr_Worldfile;

        public WFile(string filename)
        {
            // Parse the tile location out of the filename
            int p = filename.ToUpper().LastIndexOf("\\WORLD\\W");
            TileX = int.Parse(filename.Substring(p + 8, 7));
            TileZ = int.Parse(filename.Substring(p + 15, 7));

            using (SBR sbr = SBR.Open(filename))
            {
                using (SBR block = sbr.ReadSubBlock())
                {
                    Tr_Worldfile = new Tr_Worldfile(block, filename);
                }
            }
        }

        // [Rob Roeterdink] overload method added to allow selection of processed items

        public WFile(string filename, List<TokenID> reqTokens)
        {
            // Parse the tile location out of the filename
            int p = filename.ToUpper().LastIndexOf("\\WORLD\\W");
            TileX = int.Parse(filename.Substring(p + 8, 7));
            TileZ = int.Parse(filename.Substring(p + 15, 7));

            using (SBR sbr = SBR.Open(filename))
            {
                using (SBR block = sbr.ReadSubBlock())
                {
                    Tr_Worldfile = new Tr_Worldfile(block, filename, reqTokens);
                }
            }
        }

    }

    public class Tr_Worldfile : ArrayList
    {
        static HashSet<TokenID> UnknownBlockIDs = new HashSet<TokenID>()
        {
            TokenID.VDbIdCount,
            TokenID.ViewDbSphere,
        };

        public new WorldObject this[int i]
        {
            get { return (WorldObject)base[i]; }
            set { base[i] = value; }
        }

        public Tr_Worldfile(SBR block, string filename)
        {
            block.VerifyID(TokenID.Tr_Worldfile);
            var currentWatermark = 0;
            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    try
                    {

                        // [Rob Roeterdink] processing moved to subroutine to avoid duplication

                        process_worldobject(subBlock, ref currentWatermark, filename);
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(error);
                    }
                }
            }
        }

        // Overload constructor method added by Rob Roeterdink
        // Allows selection of type of items which are to be extracted, other types are ignored

        public Tr_Worldfile(SBR block, string filename, List<TokenID> reqToken)
        {
            block.VerifyID(TokenID.Tr_Worldfile);

            int currentWatermark = 0;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {

                    try
                    {
                        if (reqToken.Contains(subBlock.ID))
                        {
                            process_worldobject(subBlock, ref currentWatermark, filename);
                        }
                        else
                        {
                            subBlock.Skip();
                        }
                    }
                    catch (System.Exception error)
                    {
                        Trace.WriteLine(error);
                    }
                }
            }
        }

        // [Rob Roeterdink] set actual processing in subroutine to avoid duplication

        private void process_worldobject(SBR subBlock, ref int currentWatermark, string filename)
        {
            switch (subBlock.ID)
            {
                //some of the TokenID for binary W file:  309-->TelePole, 361-->Siding
                case TokenID.CollideObject:
                case TokenID.Static:
                    Add(new StaticObj(subBlock, currentWatermark));
                    break;
                case TokenID.TrackObj:
                    Add(new TrackObj(subBlock, currentWatermark));
                    break;
                case TokenID.CarSpawner:
                case (TokenID)357:
                    Add(new CarSpawnerObj(subBlock, currentWatermark)); //car spawner
                    break; //car spawner. The tokenid number is wrong
                case TokenID.Siding:
                case (TokenID)361:
                    Add(new SidingObj(subBlock, currentWatermark));
                    break;
                case TokenID.Platform:
                    Add(new PlatformObj(subBlock, currentWatermark));
                    break;
                case TokenID.Forest:
                    Add(new ForestObj(subBlock, currentWatermark));
                    break;
                case TokenID.LevelCr:
                    Add(new LevelCrossingObj(subBlock, currentWatermark));
                    break;
                case TokenID.Dyntrack:
                    Add(new DyntrackObj(subBlock, currentWatermark, true));
                    break;
                case (TokenID)306:
                    Add(new DyntrackObj(subBlock, currentWatermark, false));
                    break;
                case TokenID.Transfer:
                case (TokenID)363:
                    Add(new TransferObj(subBlock, currentWatermark));
                    break;
                case TokenID.Gantry:
                case (TokenID)356:
                    // TODO: Add real handling for gantry objects.
                    Add(new BaseObj(subBlock, currentWatermark));
                    break;
                case TokenID.Pickup:
                case (TokenID)359:
                    // TODO: Add real handling for pickup objects.
                    Add(new BaseObj(subBlock, currentWatermark));
                    break;
                case TokenID.Signal:
                    Add(new SignalObj(subBlock, currentWatermark));
                    break;
                case TokenID.Speedpost:
                    // TODO: Add real handling.
                    Add(new SpeedPostObj(subBlock, currentWatermark));
                    break;
                case TokenID.Tr_Watermark:
                    currentWatermark = subBlock.ReadInt();
                    break;
                default:
                    if (!UnknownBlockIDs.Contains(subBlock.ID))
                    {
                        UnknownBlockIDs.Add(subBlock.ID);
                        Trace.TraceWarning("Skipped unknown world block {0} (0x{0:X}) first seen in {1}", subBlock.ID, filename);
                    }
                    subBlock.Skip();
                    break;
            }
        }
    }


    public class BaseObj : WorldObject
    {
        public BaseObj(SBR block, int detailLevel)
        {
            //f.VerifyID(TokenID.Static); it could be CollideObject or Static object

            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }
    }

    public class StaticObj : BaseObj
    {
        public StaticObj(SBR block, int detailLevel)
            : base(block, detailLevel)
        {
        }
    }

    public class TransferObj : WorldObject
    {
        public float Width;
        public float Height;

        public TransferObj(SBR block, int detailLevel)
        {
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.Width: Width = subBlock.ReadFloat(); break;
                        case TokenID.Height: Height = subBlock.ReadFloat(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }
    }

    public class TrackObj : WorldObject
    {
        public uint SectionIdx;
        public float Elevation;
        public uint CollideFlags;
        public JNodePosn JNodePosn = null;

        public TrackObj(SBR block, int detailLevel)
        {
            block.VerifyID(TokenID.TrackObj);

            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.SectionIdx: SectionIdx = subBlock.ReadUInt(); break;
                        case TokenID.Elevation: Elevation = subBlock.ReadFloat(); break;
                        case TokenID.CollideFlags: CollideFlags = subBlock.ReadUInt(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.JNodePosn: JNodePosn = new JNodePosn(subBlock); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }
    }

    public class DyntrackObj : WorldObject
    {
        public uint SectionIdx;
        public float Elevation;
        public uint CollideFlags;
        public TrackSections trackSections;

        public DyntrackObj(SBR block, int detailLevel, bool isUnicode)
        {
            SBR localBlock = block;
            if (isUnicode)
                localBlock.VerifyID(TokenID.Dyntrack);
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.SectionIdx: SectionIdx = subBlock.ReadUInt(); break;
                        case TokenID.Elevation: Elevation = subBlock.ReadFloat(); break;
                        case TokenID.CollideFlags: CollideFlags = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.TrackSections: trackSections = new TrackSections(subBlock, isUnicode); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        // DyntrackObj copy constructor with a single TrackSection
        public DyntrackObj(DyntrackObj copy, int iTkSection)
        {
            this.SectionIdx = copy.SectionIdx;
            this.Elevation = copy.Elevation;
            this.CollideFlags = copy.CollideFlags;
            this.StaticFlags = copy.StaticFlags;
            this.Position = new STFPositionItem(copy.Position);
            this.QDirection = new STFQDirectionItem(copy.QDirection);
            this.VDbId = copy.VDbId;
            this.FileName = copy.FileName;
            this.StaticDetailLevel = copy.StaticDetailLevel;
            this.UID = copy.UID;
            //this.totalRealRun = copy.totalRealRun;
            //this.mstsTotalRise = copy.mstsTotalRise;
            // Copy only the single subsection specified
            this.trackSections = new TrackSections(copy.trackSections[iTkSection]);
        }

        public class TrackSections : ArrayList
        {
            public new TrackSection this[int i]
            {
                get { return (TrackSection)base[i]; }
                set { base[i] = value; }
            }

            // Build a TrackSections array list with one TrackSection
            public TrackSections(TrackSection TS)
            {
                this.Add(new TrackSection(TS));
            }

            public TrackSections()
            {
            }

            public TrackSections(SBR block, bool isUnicode)
            {
                block.VerifyID(TokenID.TrackSections);
                int count = 5;
                while (count-- > 0) this.Add(new TrackSection(block.ReadSubBlock(), isUnicode, count));
                block.VerifyEndOfBlock();
            }

        }//TrackSections

        public class TrackSection
        {
            // TrackSection  ==> :SectionCurve :uint,UiD :float,param1 :float,param2
            // SectionCurve  ==> :uint,isCurved
            // eg:  TrackSection (
            //	       SectionCurve ( 1 ) 40002 -0.3 120
            //      )
            // isCurve = 0 for straight, 1 for curved
            // param1 = length (m) for straight, arc (radians) for curved
            // param2 = 0 for straight, radius (m) for curved

            public uint isCurved;
            public uint UiD;
            public float param1;
            public float param2;
            public float deltaY; // Elevation change for this subsection

            public TrackSection(SBR block, bool isUnicode, int count)
            {
                block.VerifyID(TokenID.TrackSection);
                // SectionCurve
                {
                    SBR subBlock = block.ReadSubBlock();
                    if (isUnicode)
                    {
                        subBlock.VerifyID(TokenID.SectionCurve);
                        isCurved = block.ReadUInt();
                        subBlock.VerifyEndOfBlock();
                    }
                    else
                    {
                        subBlock.Skip();
                        isCurved = (uint)count % 2;
                    }
                }
                UiD = block.ReadUInt();
                param1 = block.ReadFloat();
                param2 = block.ReadFloat();
                block.VerifyEndOfBlock();
                deltaY = 0;
            }

            // Copy constructor
            public TrackSection(TrackSection copy)
            {
                this.UiD = copy.UiD;
                this.isCurved = copy.isCurved;
                this.param1 = copy.param1;
                this.param2 = copy.param2;
                this.deltaY = copy.deltaY;
            }
        }//TrackSection

    }//DyntrackObj

    public class ForestObj : WorldObject
    {
        public readonly bool IsYard;
        public readonly string TreeTexture;
        public readonly int Population;
        public readonly ScaleRange scaleRange;
        public readonly ForestArea forestArea;
        public readonly TreeSize treeSize;

        public ForestObj(SBR block, int detailLevel)
        {
            var localBlock = block;
            StaticDetailLevel = detailLevel;
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.TreeTexture: TreeTexture = subBlock.ReadString(); break;
                        case TokenID.ScaleRange: scaleRange = new ScaleRange(subBlock); break;
                        case TokenID.Area: forestArea = new ForestArea(subBlock); break;
                        case TokenID.Population: Population = subBlock.ReadInt(); break;
                        case TokenID.TreeSize: treeSize = new TreeSize(subBlock); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            IsYard = TreeTexture == null;
        }

        public class ScaleRange
        {
            public readonly float scaleRange1;
            public readonly float scaleRange2;

            internal ScaleRange(SBR block)
            {
                block.VerifyID(TokenID.ScaleRange);
                scaleRange1 = block.ReadFloat();
                scaleRange2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }

        public class ForestArea
        {
            public readonly float areaDim1;
            public readonly float areaDim2;

            internal ForestArea(SBR block)
            {
                block.VerifyID(TokenID.Area);
                areaDim1 = block.ReadFloat();
                areaDim2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }

        public class TreeSize
        {
            public readonly float treeSize1;
            public readonly float treeSize2;

            internal TreeSize(SBR block)
            {
                block.VerifyID(TokenID.TreeSize);
                treeSize1 = block.ReadFloat();
                treeSize2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }
    }

    public class SignalObj : WorldObject
    {
        public readonly uint SignalSubObj;
        public readonly SignalUnits SignalUnits;

        public SignalObj(SBR block, int detailLevel)
        {
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        case TokenID.SignalSubObj: SignalSubObj = subBlock.ReadFlags(); break;
                        case TokenID.SignalUnits: SignalUnits = new SignalUnits(subBlock); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }
    }

    public class SpeedPostObj : WorldObject
    {
        public string Speed_Digit_Tex; //ace
        public Speed_Text_Size Text_Size;// ( 0.08 0.06 0 )
        public Speed_Sign_Shape Sign_Shape;
        public List<TrItemId> trItemIDList;

        public SpeedPostObj(SBR block, int detailLevel)
        {
            block.VerifyID(TokenID.Speedpost);

            trItemIDList = new List<TrItemId>();
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.Speed_Digit_Tex: Speed_Digit_Tex = subBlock.ReadString(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.Speed_Sign_Shape: Sign_Shape = new Speed_Sign_Shape(subBlock); break;
                        case TokenID.Speed_Text_Size: Text_Size = new Speed_Text_Size(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }

        public int getTrItemID(int current, int db)
        {
            int i = 0;
            foreach (TrItemId tID in trItemIDList)
            {
                if (tID.db == db)
                {
                    if (current == i) return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class Speed_Sign_Shape
        {
            public int NumShapes;
            public float[] ShapesInfo; // ( 2 -0.021 0.481 -0.083 0 0 0.475 0.083 3.14159 )

            public Speed_Sign_Shape()
            {
            }

            public Speed_Sign_Shape(SBR block)
            {
                block.VerifyID(TokenID.Speed_Sign_Shape);
                NumShapes = block.ReadInt();
                ShapesInfo = new float[NumShapes * 4];
                for (var i = 0; i < NumShapes; i++)
                {
                    ShapesInfo[i * 4] = block.ReadFloat();
                    ShapesInfo[i * 4 + 1] = block.ReadFloat();
                    ShapesInfo[i * 4 + 2] = -block.ReadFloat();
                    ShapesInfo[i * 4 + 3] = block.ReadFloat();
                }
                block.VerifyEndOfBlock();
            }
        }
        public class Speed_Text_Size
        {
            public float Size, DX, DY;

            public Speed_Text_Size()
            {
            }

            public Speed_Text_Size(SBR block)
            {
                block.VerifyID(TokenID.Speed_Text_Size);

                Size = block.ReadFloat(); DX = block.ReadFloat(); DY = block.ReadFloat();

                block.VerifyEndOfBlock();
            }
        }

        public int getTrItemID(int current)
        {
            int i = 0;
            foreach (TrItemId tID in trItemIDList)
            {
                if (tID.db == 0)
                {
                    if (current == i) return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public int db, dbID;
            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }

    }

    //level crossing data
    public class LevelCrossingObj : WorldObject
    {
        public LevelCrParameters levelCrParameters;
        public LevelCrData levelCrData;
        public LevelCrTiming levelCrTiming;
        public List<TrItemId> trItemIDList;
        int crashProbability;
        public bool visible = true;
        public bool silent = false;
        public LevelCrossingObj(SBR block, int detailLevel)
        {

            StaticDetailLevel = detailLevel;
            trItemIDList = new List<TrItemId>();

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        case TokenID.LevelCrParameters: levelCrParameters = new LevelCrParameters(subBlock); break;
                        case TokenID.CrashProbability: crashProbability = subBlock.ReadInt(); break;
                        case TokenID.LevelCrData: levelCrData = new LevelCrData(subBlock);
                            visible = (levelCrData.crData1 & 0x1) == 0;
                            silent = !visible || (levelCrData.crData1 & 0x6) == 0x6;
                            break;
                        case TokenID.LevelCrTiming: levelCrTiming = new LevelCrTiming(subBlock); break;
                        case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public int getTrItemID(int current, int db)
        {
            int i = 0;
            foreach (TrItemId tID in trItemIDList)
            {
                if (tID.db == db)
                {
                    if (current == i) return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class LevelCrParameters
        {
            public float warningTime, minimumDistance;

            public LevelCrParameters(SBR block)
            {
                block.VerifyID(TokenID.LevelCrParameters);
                warningTime = block.ReadFloat();
                minimumDistance = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }

        public class LevelCrData
        {
            public int crData1, crData2; //not known the exact name yet
            public LevelCrData(SBR block)
            {
                block.VerifyID(TokenID.LevelCrData);
                crData1 = block.ReadInt(); // 00000001, should be taken care later, 
                crData2 = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }

        public class LevelCrTiming
        {
            public float initialTiming, seriousTiming, animTiming;
            public LevelCrTiming(SBR block)
            {
                block.VerifyID(TokenID.LevelCrTiming);
                initialTiming = block.ReadFloat();
                seriousTiming = block.ReadFloat();
                animTiming = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }
        public class TrItemId
        {
            public int db, dbID;
            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }

    }


    //Car Spawner data
    public class CarSpawnerObj : WorldObject
    {
        public List<TrItemId> trItemIDList;
        public float CarFrequency;
        public float CarAvSpeed;
        public CarSpawnerObj(SBR block, int detailLevel)
        {
            CarFrequency = 5.0f;
            CarAvSpeed = 20.0f;
            StaticDetailLevel = detailLevel;

            trItemIDList = new List<TrItemId>();

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.CarFrequency: CarFrequency = subBlock.ReadFloat(); break;
                        case TokenID.CarAvSpeed: CarAvSpeed = subBlock.ReadFloat(); break;
                        case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public int getTrItemID(int current)
        {
            int i = 0;
            foreach (TrItemId tID in trItemIDList)
            {
                if (tID.db == 1)
                {
                    if (current == i) return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public int db, dbID;
            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }
    }//CarSpawner

    /// <summary>
    /// Super-class for similar track items SidingObj and PlatformObj.
    /// </summary>
    public class TrObject : WorldObject
    {
        public List<TrItemId> trItemIDList;
        public TrObject(SBR block, int detailLevel)
        {
            StaticDetailLevel = detailLevel;

            trItemIDList = new List<TrItemId>();

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public int getTrItemID(int current)
        {
            int i = 0;
            foreach (TrItemId tID in trItemIDList)
            {
                if (tID.db == 0)
                {
                    if (current == i) return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public int db, dbID;
            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }
    }//TrObject

    /// <summary>
    /// Empty sub-class distinguishes siding objects from platform objects.
    /// </summary>
    public class SidingObj : TrObject
    {
        public SidingObj(SBR block, int detailLevel) :
            base(block, detailLevel)
        {
        }
    }//SidingObj

    /// <summary>
    /// Empty sub-class distinguishes platform objects from siding objects.
    /// </summary>
    public class PlatformObj : TrObject
    {
        public PlatformObj(SBR block, int detailLevel) :
            base(block, detailLevel)
        {
        }
    }//PlatformObj

    // These relate to the general properties settable for scenery objects in RE
    public enum StaticFlag
    {
        RoundShadow = 0x00002000,
        RectangularShadow = 0x00004000,
        TreelineShadow = 0x00008000,
        DynamicShadow = 0x00010000,
        AnyShadow = 0x0001E000,
        Terrain = 0x00040000,
        Animate = 0x00080000,
        Global = 0x00200000,
    }

    public abstract class WorldObject
    {
        public string FileName;
        public uint UID;
        public STFPositionItem Position;
        public STFQDirectionItem QDirection;
        public Matrix3x3 Matrix3x3;
        public int StaticDetailLevel = 0;
        public uint StaticFlags = 0;
        public uint VDbId;
    }


    public class STFPositionItem : TWorldPosition
    {
        public STFPositionItem(TWorldPosition p)
            : base(p)
        {
        }

        public STFPositionItem(SBR block)
        {
            block.VerifyID(TokenID.Position);
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class STFQDirectionItem : TWorldDirection
    {
        public STFQDirectionItem(TWorldDirection d)
            : base(d)
        {
        }

        public STFQDirectionItem(SBR block)
        {
            block.VerifyID(TokenID.QDirection);
            A = block.ReadFloat();
            B = block.ReadFloat();
            C = block.ReadFloat();
            D = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class Matrix3x3
    {
        public Matrix3x3(SBR block)
        {
            block.VerifyID(TokenID.Matrix3x3);
            AX = block.ReadFloat();
            AY = block.ReadFloat();
            AZ = block.ReadFloat();
            BX = block.ReadFloat();
            BY = block.ReadFloat();
            BZ = block.ReadFloat();
            CX = block.ReadFloat();
            CY = block.ReadFloat();
            CZ = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
        public float AX, AY, AZ, BX, BY, BZ, CX, CY, CZ;
    }

    public class JNodePosn
    {
        public int TileX, TileZ;
        public float X, Y, Z;

        public JNodePosn(SBR block)
        {
            block.VerifyID(TokenID.JNodePosn);
            TileX = block.ReadInt();
            TileZ = block.ReadInt();
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class TWorldDirection
    {
        public float A;
        public float B;
        public float C;
        public float D;
        public TWorldDirection(float a, float b, float c, float d) { A = a; B = b; C = c; D = d; }
        public TWorldDirection() { A = 0; B = 0; C = 0; D = 1; }
        public TWorldDirection(TWorldDirection d) { A = d.A; B = d.B; C = d.C; D = d.D; }

        public void SetBearing(float compassRad)
        {
            float slope = GetSlope();
            SetAngles(compassRad, slope);
        }

        public void SetBearing(float dx, float dz)
        {
            float slope = GetSlope();
            float compassRad = M.AngleDxDz(dx, dz);
            SetAngles(compassRad, slope);
        }

        public void Rotate(float radians)  // Rotate around world vertical axis - +degrees is clockwise
        // This rotates about the surface normal
        {
            SetBearing(GetBearing() + radians);
        }

        public void Pivot(float radians)	// This rotates about object Y axis
        {
            radians += GetBearing();
            float slope = GetSlope();
            SetAngles(radians, -slope);
        }

        public void SetAngles(float compassRad, float tiltRad)  // + rad is tilted up or rotated east
        // from http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/
        /*
         *  w = Math.sqrt(1.0 + C1 * C2 + C1*C3 - S1 * S2 * S3 + C2*C3) / 2
            x = (C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w) 
            y = (S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w)
            z = (-S1 * S3 + C1 * S2 * C3 + S2) /(4.0 * w) 


            where:

            C1 = cos(heading) 
            C2 = cos(attitude) 
            C3 = cos(bank) 
            S1 = sin(heading) 
            S2 = sin(attitude) 
            S3 = sin(bank)     it seems in MSTS - tilt forward back is bank
				
        Applied in order of heading, attitude then bank 
        */
        {
            float a1 = compassRad;
            float a2 = 0;
            float a3 = tiltRad;

            float C1 = (float)Math.Cos(a1);
            float S1 = (float)Math.Sin(a1);
            float C2 = (float)Math.Cos(a2);
            float S2 = (float)Math.Sin(a2);
            float C3 = (float)Math.Cos(a3);
            float S3 = (float)Math.Sin(a3);

            float w = (float)Math.Sqrt(1.0 + C1 * C2 + C1 * C3 - S1 * S2 * S3 + C2 * C3) / 2.0f;
            float x;
            float y;
            float z;

            if (Math.Abs(w) < .000005)
            {
                A = 0.0f;
                B = -1.0f;
                C = 0.0f;
                D = 0.0f;
            }
            else
            {
                x = (float)(-(C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w));
                y = (float)(-(S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w));
                z = (float)(-(-S1 * S3 + C1 * S2 * C3 + S2) / (4.0 * w));

                A = x;
                B = y;
                C = z;
                D = w;
            }
        }

        public void SetSlope(float tiltRad) // +v is tilted up
        {
            float compassAngleRad = M.AngleDxDz(DX(), DZ());
            SetAngles(compassAngleRad, tiltRad);
        }

        public void Tilt(float radians)   // Tilt up the specified number of radians
        {
            SetSlope(GetSlope() + radians);
        }

        public void MakeLevel()  // Remove any tilt from the direction.
        {
            SetSlope(0);
        }
        public float DY()
        {

            float x = -A; // imaginary i part of quaternion
            float y = -B; // imaginary j part of quaternion
            float z = -C; // imaginary k part of quaternion
            float w = D; // real part of quaternionfloat 


            //From http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
            //p2.x = ( w*w*p1.x + 2*y*w*p1.z - 2*z*w*p1.y + x*x*p1.x + 2*y*x*p1.y + 2*z*x*p1.z - z*z*p1.x - y*y*p1.x );	
            //p2.y = ( 2*x*y*p1.x + y*y*p1.y + 2*z*y*p1.z + 2*w*z*p1.x - z*z*p1.y + w*w*p1.y - 2*x*w*p1.z - x*x*p1.y );	
            //p2.z = ( 2*x*z*p1.x + 2*y*z*p1.y + z*z*p1.z - 2*w*y*p1.x - y*y*p1.z + 2*w*x*p1.y - x*x*p1.z + w*w*p1.z );

            float dy = (2 * z * y - 2 * x * w);
            return dy;
        }

        public float DX()
        {

            // WAS return -2.0*B*D; 

            /* Was
            float x = C;
            float y = A;
            float z = B;
            float w = D;

            return -2.0 * ( x * y + z * w );
            */

            float x = -A;
            float y = -B;
            float z = -C;
            float w = D;

            float dX = (2 * y * w + 2 * z * x);
            return dX;

        }
        public float DZ()
        {
            float x = -A;
            float y = -B;
            float z = -C;
            float w = D;

            return z * z - y * y - x * x + w * w;

        }
        public float GetSlope()   // Return the slope, +radians is tilted up
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            float qx = -A;
            float qy = -B;
            float qz = -C;
            float qw = D;

            //float heading;
            //float attitude;
            float bank;

            if (Math.Abs(qx * qy + qz * qw - 0.5) < .00001)
            {
                //heading = 2 * Math.Atan2(qx,qw);
                bank = 0;
            }
            else if (Math.Abs(qx * qy + qz * qw + 0.5) < .00001)
            {
                //heading = -2 * Math.Atan2(qx,qw);
                bank = 0;
            }

            //heading = Math.Atan2(2*qy*qw-2*qx*qz , 1 - 2*qy*qy - 2*qz*qz);
            //attitude = Math.Asin(2*qx*qy + 2*qz*qw);
            bank = (float)Math.Atan2(2 * qx * qw - 2 * qy * qz, 1 - 2 * qx * qx - 2 * qz * qz);

            return bank;
        }

        /* OLD METHOD
        public float GetBearing( )  // Return the compass bearing +radians is east of north
        {
            return M.AngleDxDz( DX(), DZ() );
        }
        */

        public float GetBearing()   // Return the bearing
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            float qx = -A;
            float qy = -B;
            float qz = -C;
            float qw = D;

            float heading;
            //float attitude;
            //float bank;

            if (Math.Abs(qx * qy + qz * qw - 0.5) < .00001)
            {
                heading = 2f * (float)Math.Atan2(qx, qw);
                //bank = 0;
            }
            else if (Math.Abs(qx * qy + qz * qw + 0.5) < .00001)
            {
                heading = -2f * (float)Math.Atan2(qx, qw);
                //bank = 0;
            }
            else
            {
                heading = (float)Math.Atan2(2 * qy * qw - 2 * qx * qz, 1 - 2 * qy * qy - 2 * qz * qz);
                //attitude = Math.Asin(2*qx*qy + 2*qz*qw);
                //bank = Math.Atan2(2*qx*qw-2*qy*qz , 1 - 2*qx*qx - 2*qz*qz);
            }

            return heading;
        }

        public static float AngularDistance(TWorldDirection d1, TWorldDirection d2)
        // number of radians separating angle one and angle two - always positive
        {
            float a1 = d1.GetBearing();
            float a2 = d2.GetBearing();

            float a = a1 - a2;

            a = Math.Abs(a);

            while (a > Math.PI)
                a -= 2.0f * (float)Math.PI;

            return (float)Math.Abs(a);
        }

        /// <summary>
        /// Rotate the specified point in model space to a new location according to the quaternion 
        /// Center of rotation is 0,0,0 in model space
        /// Example   xyz = 0,1,2 rotated 90 degrees east becomes 2,1,0
        /// </summary>
        /// <param name="p1"></param>
        private TWorldPosition RotatePoint(TWorldPosition p1)
        {

            float x = -A; // imaginary i part of quaternion
            float y = -B; // imaginary j part of quaternion
            float z = -C; // imaginary k part of quaternion
            float w = D; // real part of quaternionfloat 

            TWorldPosition p2 = new TWorldPosition();

            p2.X = (w * w * p1.X + 2 * y * w * p1.Z - 2 * z * w * p1.Y + x * x * p1.X + 2 * y * x * p1.Y + 2 * z * x * p1.Z - z * z * p1.X - y * y * p1.X);
            p2.Y = (2 * x * y * p1.X + y * y * p1.Y + 2 * z * y * p1.Z + 2 * w * z * p1.X - z * z * p1.Y + w * w * p1.Y - 2 * x * w * p1.Z - x * x * p1.Y);
            p2.Z = (2 * x * z * p1.X + 2 * y * z * p1.Y + z * z * p1.Z - 2 * w * y * p1.X - y * y * p1.Z + 2 * w * x * p1.Y - x * x * p1.Z + w * w * p1.Z);

            return p2;
        }

    }

    public class TWorldPosition
    {
        public float X;
        public float Y;
        public float Z;
        public TWorldPosition(float x, float y, float z) { X = x; Y = y; Z = z; }
        public TWorldPosition() { X = 0.0f; Y = 0.0f; Z = 0.0f; }
        public TWorldPosition(TWorldPosition p)
        {
            X = p.X;
            Y = p.Y;
            Z = p.Z;
        }

        public void Move(TWorldDirection q, float distance)
        {

            X += (q.DX() * distance);
            Y += (q.DY() * distance);
            Z += (q.DZ() * distance);
        }

        public void Offset(TWorldDirection d, float distanceRight)
        {
            TWorldDirection DRight = new TWorldDirection(d);
            DRight.Rotate(M.Radians(90));
            Move(DRight, distanceRight);
        }

        public static float PointDistance(TWorldPosition p1, TWorldPosition p2)
        // distance between p1 and p2 along the surface
        {
            float dX = p1.X - p2.X;
            float dZ = p1.Z - p2.Z;
            return (float)Math.Sqrt(dX * dX + dZ * dZ);
        }
    }

    public class SignalUnits
    {
        public readonly SignalUnit[] Units;

        public SignalUnits(SBR block)
        {
            var units = new List<SignalUnit>();
            block.VerifyID(TokenID.SignalUnits);
            var count = block.ReadUInt();
            for (var i = 0; i < count; i++)
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    units.Add(new SignalUnit(subBlock));
                }
            }
            block.VerifyEndOfBlock();
            Units = units.ToArray();
        }
    }

    public class SignalUnit
    {
        public readonly int SubObj;
        public readonly uint TrItem;

        public SignalUnit(SBR block)
        {
            block.VerifyID(TokenID.SignalUnit);
            SubObj = block.ReadInt();
            using (SBR subBlock = block.ReadSubBlock())
            {
                subBlock.VerifyID(TokenID.TrItemId);
                subBlock.ReadUInt(); // Unk?
                TrItem = subBlock.ReadUInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }
}
