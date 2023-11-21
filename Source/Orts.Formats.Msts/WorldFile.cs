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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using ORTS.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Orts.Formats.Msts
{
    public class WorldFile
    {
        public readonly int TileX, TileZ;
        public readonly Tr_Worldfile Tr_Worldfile;

        public WorldFile(string filename)
            : this(filename, null)
        {
        }

        public WorldFile(string filename, List<TokenID> allowedTokens)
        {
            try
            {
                // Parse the tile location out of the filename.
                var p = filename.ToUpper().LastIndexOf("\\WORLD\\W");
                TileX = int.Parse(filename.Substring(p + 8, 7));
                TileZ = int.Parse(filename.Substring(p + 15, 7));

                using (var sbr = SBR.Open(filename))
                {
                    using (var block = sbr.ReadSubBlock())
                    {
                        Tr_Worldfile = new Tr_Worldfile(block, filename, allowedTokens);
                    }
                    // some w files have additional comments at the end 
                    //       eg _Skip ( "TS DB-Utility - Version: 3.4.05(13.10.2009), Filetype='World', Copyright (C) 2003-2009 by ...CarlosHR..." )
                    sbr.Skip();
                }
            }
            catch (Exception error)
            {
                throw new FileLoadException(filename, error);
            }
        }

        public void InsertORSpecificData (string filename, List<TokenID> allowedTokens)
        {
            using (var sbr = SBR.Open(filename))
            {
                using (var block = sbr.ReadSubBlock())
                {
                    Tr_Worldfile.InsertORSpecificData(block, filename, allowedTokens);
                }
                // some w files have additional comments at the end 
                //       eg _Skip ( "TS DB-Utility - Version: 3.4.05(13.10.2009), Filetype='World', Copyright (C) 2003-2009 by ...CarlosHR..." )
                sbr.Skip();
            }
        }
    }

    public class Tr_Worldfile : List<WorldObject>
    {
        static HashSet<TokenID> UnknownBlockIDs = new HashSet<TokenID>()
        {
            TokenID.VDbIdCount,
            TokenID.ViewDbSphere,
        };

        public Tr_Worldfile(SBR block, string filename, List<TokenID> allowedTokens)
        {
            block.VerifyID(TokenID.Tr_Worldfile);
            var currentWatermark = 0;
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    try
                    {
                        if (allowedTokens == null || allowedTokens.Contains(subBlock.ID))
                            LoadObject(subBlock, ref currentWatermark, filename);
                        else
                            subBlock.Skip();
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(filename, error));
                    }
                }
            }
        }

        public void Serialize(StringBuilder sb)
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            sb.Append("﻿SIMISA@@@@@@@@@@JINX0w0t______");
            sb.AppendLine();

            var watermark = 0;
            Sort((a, b) => a.StaticDetailLevel.CompareTo(b.StaticDetailLevel));
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var currentWatermark = enumerator.Current.StaticDetailLevel;
                if (currentWatermark != watermark)
                {
                    watermark = currentWatermark;
                    sb.AppendLine();
                    sb.Append("    " + TokenID.Tr_Watermark + "( " + currentWatermark + " )");
                }
                switch (enumerator.Current)
                {
                    case StaticObj staticObj: staticObj.Serialize(sb); break;
                    case TrackObj trackObj: trackObj.Serialize(sb); break;
                    case CarSpawnerObj carSpawnerObj: carSpawnerObj.Serialize(sb); break;
                    case SidingObj sidingObj: sidingObj.Serialize(sb); break;
                    case PlatformObj platformObj: platformObj.Serialize(sb); break;
                    case ForestObj forestObj: forestObj.Serialize(sb); break;
                    case LevelCrossingObj levelCrossingObj: levelCrossingObj.Serialize(sb); break;
                    case DyntrackObj dyntrackObj: dyntrackObj.Serialize(sb); break;
                    case TransferObj transferObj: transferObj.Serialize(sb); break;
                    case PickupObj pickupObj: pickupObj.Serialize(sb); break;
                    case HazardObj hazardObj: hazardObj.Serialize(sb); break;
                    case SignalObj signalObj: signalObj.Serialize(sb); break;
                    case SpeedPostObj speedPostObj: speedPostObj.Serialize(sb); break;
                }
            }
            Thread.CurrentThread.CurrentCulture = culture;
        }

        void LoadObject(SBR subBlock, ref int currentWatermark, string filename)
        {
            switch (subBlock.ID)
            {
                case TokenID.CollideObject:
                case TokenID.Static:
                    Add(new StaticObj(subBlock, currentWatermark));
                    break;
                case TokenID.TrackObj:
                    Add(new TrackObj(subBlock, currentWatermark));
                    break;
                case TokenID.CarSpawner:
                case (TokenID)357:
                    Add(new CarSpawnerObj(subBlock, currentWatermark));
                    break;
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
                case (TokenID)306:
                    Add(new DyntrackObj(subBlock, currentWatermark));
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
                    Add(new PickupObj(subBlock, currentWatermark));
                    break;
                case TokenID.Hazard:
                    //case (TokenID)359:
                    Add(new HazardObj(subBlock, currentWatermark));
                    break;
                case TokenID.Signal:
                    Add(new SignalObj(subBlock, currentWatermark));
                    break;
                case TokenID.Speedpost:
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

        public void InsertORSpecificData (SBR block, string filename, List<TokenID> allowedTokens)
        {
            block.VerifyID(TokenID.Tr_Worldfile);
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    try
                    {
                        if (allowedTokens == null || allowedTokens.Contains(subBlock.ID))
                        {
                            WorldObject origObject;
                            bool wrongBlock = false;
                            if (!subBlock.EndOfBlock())
                            {
                                var subSubBlockUID = subBlock.ReadSubBlock();
                                // check if a block with this UiD already present
                                if (subSubBlockUID.ID == TokenID.UiD)
                                {
                                    uint UID = subSubBlockUID.ReadUInt();
                                    origObject = Find(x => x.UID == UID);
                                    if (origObject == null)
                                    {
                                        wrongBlock = true;
                                        Trace.TraceWarning("Skipped world block {0} (0x{0:X}), UID {1} not matching with base file", subBlock.ID, UID);
                                        subSubBlockUID.Skip();
                                        subBlock.Skip();
                                    }
                                    else
                                    {
                                        wrongBlock = !TestMatch(subBlock, origObject);
                                        if (!wrongBlock)
                                        {
                                            subSubBlockUID.Skip();
                                            while (!subBlock.EndOfBlock() && !wrongBlock)
                                            {
                                                using (var subSubBlock = subBlock.ReadSubBlock())
                                                {

                                                    origObject.AddOrModifyObj(subSubBlock);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Trace.TraceWarning("Skipped world block {0} (0x{0:X}), UID {1} not matching with base file", subBlock.ID, UID);
                                            subSubBlockUID.Skip();
                                            subBlock.Skip();
                                        }
                                    }
                                }
 
                            }
                            subBlock.EndOfBlock();
                        }
                        else
                            subBlock.Skip();
                    }

                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(filename, error));
                    }
                }
            }
        }

        private bool TestMatch(SBR subBlock, WorldObject origObject)
        {
            if (subBlock.ID == TokenID.Static && origObject is StaticObj) return true;
            if (subBlock.ID == TokenID.Gantry && origObject is BaseObj) return true;
            if (subBlock.ID == TokenID.Pickup && origObject is PickupObj) return true;
            if (subBlock.ID == TokenID.Transfer && origObject is TransferObj) return true;
            if (subBlock.ID == TokenID.Forest && origObject is ForestObj) return true;
            if (subBlock.ID == TokenID.Signal && origObject is SignalObj) return true;
            if (subBlock.ID == TokenID.Speedpost && origObject is SpeedPostObj) return true;
            if (subBlock.ID == TokenID.LevelCr && origObject is LevelCrossingObj) return true;
            if (subBlock.ID == TokenID.Hazard && origObject is HazardObj) return true;
            if (subBlock.ID == TokenID.CarSpawner && origObject is CarSpawnerObj) return true;
            return false;
        }
    }

    public class BaseObj : WorldObject
    {
        public BaseObj()
        {
        }

        public BaseObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Gantry;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }
        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
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

    public class StaticObj : BaseObj
    {
        public StaticObj(SBR block, int detailLevel)
            : base(block, detailLevel)
        {
            TokenID = TokenID.Static;
        }

        public override void AddOrModifyObj(SBR subBlock)
        {
            base.AddOrModifyObj(subBlock);
        }
    }

    /// <summary>
    /// Pickup objects supply fuel (diesel, coal) or water.
    /// </summary>
    public class PickupObj : BaseObj
    {
        public uint PickupType;
        public PickupAnimDataItem PickupAnimData;
        public SpeedRangeItem SpeedRange;
        public PickupCapacityItem PickupCapacity;
        public List<TrItemId> TrItemIDList = new List<TrItemId>();
        public uint CollideFlags;
        public int MaxStackedContainers;
        public float StackLocationsLength = 12.19f;
        public StackLocationItems StackLocations;
        public float PickingSurfaceYOffset;
        public Vector3 PickingSurfaceRelativeTopStartPosition;
        public int GrabberArmsParts = 2;
        public string CraneSound;

        public WorldLocation Location;

        /// <summary>
        /// Creates the object, but currently skips the animation field.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="detailLevel"></param>
        public PickupObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Pickup;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
        }
            
        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
                case TokenID.SpeedRange: SpeedRange = new SpeedRangeItem(subBlock); break;
                case TokenID.PickupType: PickupType = subBlock.ReadUInt();
                    subBlock.Skip(); // Discard the 2nd value (0 or 1 but significance is not known)
                    break;
                case TokenID.ORTSMaxStackedContainers: MaxStackedContainers = subBlock.ReadInt(); break;
                case TokenID.ORTSStackLocationsLength: StackLocationsLength = subBlock.ReadFloat(); break;
                case TokenID.ORTSStackLocations: StackLocations = new StackLocationItems(subBlock, this); break;
                case TokenID.ORTSPickingSurfaceYOffset: PickingSurfaceYOffset = subBlock.ReadFloat(); break;
                case TokenID.ORTSPickingSurfaceRelativeTopStartPosition: PickingSurfaceRelativeTopStartPosition = subBlock.ReadVector3(); break;
                case TokenID.ORTSGrabberArmsParts: GrabberArmsParts = subBlock.ReadInt(); break;
                case TokenID.ORTSCraneSound: CraneSound = subBlock.ReadString(); break;
                case TokenID.PickupAnimData: PickupAnimData = new PickupAnimDataItem(subBlock); break;
                case TokenID.PickupCapacity: PickupCapacity = new PickupCapacityItem(subBlock); break;
                case TokenID.TrItemId: TrItemIDList.Add(new TrItemId(subBlock)); break;
                case TokenID.CollideFlags: CollideFlags = subBlock.ReadUInt(); break;
                case TokenID.FileName: FileName = subBlock.ReadString(); break;
                case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                default: subBlock.Skip(); break;
            }
        }


        /// <summary>
        /// SpeedRangeItem specifies the acceptable range of speeds (meters/sec) for using a pickup.
        /// Presumably non-zero speeds are intended for water troughs or, perhaps, merry-go-round freight.
        /// </summary>
        public class SpeedRangeItem
        {
            public readonly float MinMpS;
            public readonly float MaxMpS;

            internal SpeedRangeItem(SBR block)
            {
                block.VerifyID(TokenID.SpeedRange);
                MinMpS = block.ReadFloat();
                MaxMpS = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(MinMpS.ToString("F") + " " + MaxMpS.ToString("F")); }
        }

        /// <summary>
        /// PickupAnimDataItem specifies 2 values.  The first represents different pickup animation options.
        /// The second represents the animation speed which will be used.
        /// For the moment PickupOptions may not be used.
        /// </summary>
        public class PickupAnimDataItem
        {
            public readonly float PickupOptions;
            public readonly float AnimationSpeed;

            internal PickupAnimDataItem(SBR block)
            {
                block.VerifyID(TokenID.PickupAnimData);
                PickupOptions = block.ReadFloat();
                AnimationSpeed = block.ReadFloat();
                if (AnimationSpeed == 0) AnimationSpeed = 1.0f;
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(PickupOptions.ToString("F") + " " + AnimationSpeed.ToString("F")); }
        }

        /// <summary>
        /// Creates the object.
        /// The units of measure have been assumed and, once parsed, the values are not currently used.
        /// </summary>
        public class PickupCapacityItem
        {
            public readonly float QuantityAvailableKG;
            public readonly float FeedRateKGpS;

            internal PickupCapacityItem(SBR block)
            {
                block.VerifyID(TokenID.PickupCapacity);
                QuantityAvailableKG = Kg.FromLb(block.ReadFloat());
                FeedRateKGpS = Kg.FromLb(block.ReadFloat());
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(QuantityAvailableKG.ToString("F") + " " + FeedRateKGpS.ToString("F")); }
        }

        public class StackLocationItems
        {
            public readonly StackLocation[] Locations;

            public StackLocationItems(SBR block, PickupObj pickupObj)
            {
                var locations = new List<StackLocation>();
                var count = block.ReadUInt();
                for (var i = 0; i < count; i++)
                {
                    using (var subBlock = block.ReadSubBlock())
                    {
                        if (subBlock.ID == TokenID.StackLocation)
                        {
                            locations.Add(new StackLocation(subBlock));
                        }
                    }
                    if (locations[i].Length == 0) locations[i].Length = pickupObj.StackLocationsLength;
                    if (locations[i].MaxStackedContainers == 0) locations[i].MaxStackedContainers = pickupObj.MaxStackedContainers;
                }
                block.VerifyEndOfBlock();
                Locations = locations.ToArray();
                locations.Clear();
            }

            public void Serialize(StringBuilder sb)
            {
                sb.AppendLine();
                foreach (var location in Locations)
                {
                    sb.Append("        Location (");
                    sb.AppendLine();
                    location.Serialize(sb);
                    sb.AppendLine();
                    sb.Append("        )");
                }
            }
        }

        public class StackLocation
        {
            public Vector3 Position;
            public int MaxStackedContainers;
            public float Length;
            public bool Flipped;

            public StackLocation(SBR block)
            {
                while (!block.EndOfBlock())
                {
                    using (var subBlock = block.ReadSubBlock())
                    {
                        switch (subBlock.ID)
                        {
                            case TokenID.Position: Position = subBlock.ReadVector3(); break;
                            case TokenID.MaxStackedContainers: MaxStackedContainers = subBlock.ReadInt(); break;
                            case TokenID.Length: Length = subBlock.ReadFloat(); break;
                            case TokenID.Flipped: Flipped = subBlock.ReadInt() == 1; break;
                            default: subBlock.Skip(); break;
                        }
                    }
                }
            }

            public void Serialize(StringBuilder sb)
            {
                sb.Append("            Position ( " + Position.ToString() + " )");
                sb.Append("            MaxStackedContainers ( " + MaxStackedContainers + " )");
                sb.Append("            Length ( " + Length.ToString("F") + " )");
                sb.Append("            Flipped ( " + (Flipped ? "1" : "0") + " )");
            }
        }
    }

    public class TransferObj : WorldObject
    {
        public float Width;
        public float Height;

        public TransferObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Transfer;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
        }

        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
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

    public class TrackObj : WorldObject
    {
        public uint SectionIdx;
        public float Elevation;
        public uint CollideFlags;
        public JNodePosn JNodePosn;

        public TrackObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.TrackObj;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);

            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }

        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
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

    public class DyntrackObj : WorldObject
    {
        public readonly uint SectionIdx;
        public readonly float Elevation;
        public readonly uint CollideFlags;
        public readonly TrackSections trackSections;

        public DyntrackObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Dyntrack;
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
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
                        case TokenID.TrackSections: trackSections = new TrackSections(subBlock); break;
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

        public class TrackSections : List<TrackSection>
        {
            public TrackSections()
            {
            }

            public TrackSections(TrackSection trackSection)
            {
                Add(new TrackSection(trackSection));
            }

            public TrackSections(SBR block)
            {
                block.VerifyID(TokenID.TrackSections);
                var count = 5;
                while (count-- > 0)
                    Add(new TrackSection(block.ReadSubBlock(), count));
                block.VerifyEndOfBlock();
            }
        }

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

            public readonly uint isCurved;
            public readonly uint UiD;
            public readonly float param1;
            public readonly float param2;
            public readonly float deltaY; // Elevation change for this subsection

            public TrackSection(SBR block, int count)
            {
                block.VerifyID(TokenID.TrackSection);
                using (var subBlock = block.ReadSubBlock())
                {
                    subBlock.VerifyID(TokenID.SectionCurve);
                    isCurved = subBlock.ReadUInt();
                    subBlock.VerifyEndOfBlock();
                }
                UiD = block.ReadUInt();
                param1 = block.ReadFloat();
                param2 = block.ReadFloat();
                block.VerifyEndOfBlock();
                deltaY = 0;
            }

            public TrackSection(TrackSection copy)
            {
                this.UiD = copy.UiD;
                this.isCurved = copy.isCurved;
                this.param1 = copy.param1;
                this.param2 = copy.param2;
                this.deltaY = copy.deltaY;
            }
         
            public void Serialize(StringBuilder sb)
            {
                sb.Append("SectionCurve ( " + isCurved + " ) " + UiD + " " + param1.ToString("F") + " " + param2.ToString("F"));
                if (deltaY != 0)
                    sb.Append(" " + deltaY);
            }
        }
    }

    public class ForestObj : WorldObject
    {
        public bool IsYard;
        public string TreeTexture;
        public int Population;
        public ScaleRange scaleRange;
        public ForestArea forestArea;
        public TreeSize treeSize;

        public ForestObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Forest;
            StaticDetailLevel = detailLevel;

            ReadBlock (block);

            IsYard = TreeTexture == null;
        }


        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
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
        public class ScaleRange
        {
            public readonly float Minimum;
            public readonly float Maximum;

            internal ScaleRange(SBR block)
            {
                block.VerifyID(TokenID.ScaleRange);
                Minimum = block.ReadFloat();
                Maximum = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(Minimum.ToString("F") + " " + Maximum.ToString("F")); }
        }

        public class ForestArea
        {
            public readonly float X;
            public readonly float Z;

            internal ForestArea(SBR block)
            {
                block.VerifyID(TokenID.Area);
                X = block.ReadFloat();
                Z = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(X.ToString("F") + " " + Z.ToString("F")); }
        }

        public class TreeSize
        {
            public readonly float Width;
            public readonly float Height;

            internal TreeSize(SBR block)
            {
                block.VerifyID(TokenID.TreeSize);
                Width = block.ReadFloat();
                Height = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(Width.ToString("F") + " " + Height.ToString("F")); }
        }
    }

    public class SignalObj : WorldObject
    {
        public uint SignalSubObj;
        public SignalUnits SignalUnits;

        public SignalObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Signal;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
        }

        public override void AddOrModifyObj(SBR subBlock)
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

    public class SpeedPostObj : WorldObject
    {
        public string Speed_Digit_Tex; //ace
        public Speed_Text_Size Text_Size;// ( 0.08 0.06 0 )
        public Speed_Sign_Shape Sign_Shape;
        public List<TrItemId> trItemIDList = new List<TrItemId>();

        public SpeedPostObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Speedpost;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);

        }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop

        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
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


        public class Speed_Sign_Shape
        {
            public readonly int NumShapes;
            public readonly float[] ShapesInfo; // ( 2 -0.021 0.481 -0.083 0 0 0.475 0.083 3.14159 )

            public Speed_Sign_Shape(SBR block)
            {
                block.VerifyID(TokenID.Speed_Sign_Shape);
                NumShapes = block.ReadInt();
                ShapesInfo = new float[NumShapes * 4];
                for (var i = 0; i < NumShapes; i++)
                {
                    ShapesInfo[i * 4 + 0] = block.ReadFloat();
                    ShapesInfo[i * 4 + 1] = block.ReadFloat();
                    ShapesInfo[i * 4 + 2] = -block.ReadFloat();
                    ShapesInfo[i * 4 + 3] = block.ReadFloat();
                }
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb)
            {
                sb.Append(NumShapes);
                foreach (var shapeInfo in ShapesInfo)
                {
                    sb.Append(" " + shapeInfo.ToString("F"));
                }
            }
        }

        public class Speed_Text_Size
        {
            public readonly float Size, DX, DY;

            public Speed_Text_Size(SBR block)
            {
                block.VerifyID(TokenID.Speed_Text_Size);
                Size = block.ReadFloat();
                DX = block.ReadFloat();
                DY = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(Size.ToString("F") + " " + DX.ToString("F") + " " + DY.ToString("F")); }
        }

        public int GetTrItemID(int index)
        {
            var i = 0;
            foreach (var tID in trItemIDList)
            {
                if (tID.db == 0)
                {
                    if (index == i)
                        return tID.dbID;
                    i++;
                }
            }
            return -1;
        }
    }

    public class TrItemId
    {
        public readonly int db, dbID;

        public TrItemId(SBR block)
        {
            block.VerifyID(TokenID.TrItemId);
            db = block.ReadInt();
            dbID = block.ReadInt();
            block.VerifyEndOfBlock();
        }

        public void Serialize(StringBuilder sb) { sb.Append(db + " " + dbID); }
    }

    public class LevelCrossingObj : WorldObject
    {
        public LevelCrParameters levelCrParameters;
        public LevelCrData levelCrData;
        public LevelCrTiming levelCrTiming;
        public List<TrItemId> trItemIDList = new List<TrItemId>();
        public int crashProbability;
        public bool visible = true;
        public bool silent = false;
        public string SoundFileName = "";

        public LevelCrossingObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.LevelCr;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
        }

    public override void AddOrModifyObj(SBR subBlock)
    {
        switch (subBlock.ID)
        {
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
            case TokenID.ORTSSoundFileName: SoundFileName = subBlock.ReadString(); break;
            default: subBlock.Skip(); break;
        }
    }

        public class LevelCrParameters
        {
            public readonly float warningTime, minimumDistance;

            public LevelCrParameters(SBR block)
            {
                block.VerifyID(TokenID.LevelCrParameters);
                warningTime = block.ReadFloat();
                minimumDistance = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(warningTime.ToString("F") + " " + minimumDistance.ToString("F")); }
        }

        public class LevelCrData
        {
            public readonly int crData1, crData2; //not known the exact name yet

            public LevelCrData(SBR block)
            {
                block.VerifyID(TokenID.LevelCrData);
                crData1 = block.ReadInt(); // 00000001, should be taken care later, 
                crData2 = block.ReadInt();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(crData1 + " " + crData2); }
        }

        public class LevelCrTiming
        {
            public readonly float initialTiming, seriousTiming, animTiming;

            public LevelCrTiming(SBR block)
            {
                block.VerifyID(TokenID.LevelCrTiming);
                initialTiming = block.ReadFloat();
                seriousTiming = block.ReadFloat();
                animTiming = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(initialTiming.ToString("F") + " " + seriousTiming.ToString("F") + " " + animTiming.ToString("F")); }
        }

        public class TrItemId
        {
            public readonly int db, dbID;

            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(db + " " + dbID); }
        }
    }

    public class HazardObj : WorldObject
    {
        public int TrItemId;

        public HazardObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Hazard;
            StaticDetailLevel = detailLevel;

            ReadBlock(block);
        }

        public override void AddOrModifyObj(SBR subBlock)
        {
            switch (subBlock.ID)
            {
                case TokenID.TrItemId: TrItemId = DecodeTrItemId(subBlock); break;
                case TokenID.FileName: FileName = subBlock.ReadString(); break;
                case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                default: subBlock.Skip(); break;
            }
        }

        int DecodeTrItemId(SBR block)
        {
            block.VerifyID(TokenID.TrItemId);
            int db = block.ReadInt();
            int dbID = block.ReadInt();
            block.VerifyEndOfBlock();
            return dbID;
        }
    }

    public class CarSpawnerObj : WorldObject
    {
        public readonly List<TrItemId> trItemIDList = new List<TrItemId>();
        public float CarFrequency;
        public float CarAvSpeed;
        public string ListName; // name of car list associated to this car spawner
        public int CarSpawnerListIdx;

        public CarSpawnerObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.CarSpawner;
            StaticDetailLevel = detailLevel;
            CarFrequency = 5.0f;
            CarAvSpeed = 20.0f;

            ReadBlock (block);
        }

        public override void AddOrModifyObj (SBR subBlock)
        {
            switch (subBlock.ID)
            {
                case TokenID.CarFrequency: CarFrequency = subBlock.ReadFloat(); break;
                case TokenID.CarAvSpeed: CarAvSpeed = subBlock.ReadFloat(); break;
                case TokenID.ORTSListName: ListName = subBlock.ReadString(); break;
                case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                default: subBlock.Skip(); break;
            }
        }


        public int getTrItemID(int index)
        {
            var i = 0;
            foreach (var tID in trItemIDList)
            {
                if (tID.db == 1)
                {
                    if (index == i)
                        return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public readonly int db, dbID;

            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(db + " " + dbID); }
        }
    }

    /// <summary>
    /// Super-class for similar track items SidingObj and PlatformObj.
    /// </summary>
    public class TrObject : WorldObject
    {
        public readonly List<TrItemId> trItemIDList = new List<TrItemId>();

        // this one called by PlatformObj
        public TrObject()
        { }

        // this one called by SidingObj
        public TrObject(SBR block, int detailLevel)
        {
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
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

        public int getTrItemID(int index)
        {
            var i = 0;
            foreach (var tID in trItemIDList)
            {
                if (tID.db == 0)
                {
                    if (index == i)
                        return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public readonly int db, dbID;

            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }

            public void Serialize(StringBuilder sb) { sb.Append(db + " " + dbID); }
        }
    }

    /// <summary>
    /// Empty sub-class distinguishes siding objects from platform objects.
    /// </summary>
    public class SidingObj : TrObject
    {
        public SidingObj(SBR block, int detailLevel) :
            base(block, detailLevel)
        {
            TokenID = TokenID.Siding;
        }
    }

    /// <summary>
    /// Empty sub-class distinguishes platform objects from siding objects.
    /// </summary>
    public class PlatformObj : TrObject
    {
        public uint PlatformData;

        public PlatformObj(SBR block, int detailLevel)
        {
            TokenID = TokenID.Platform;
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.PlatformData: PlatformData = subBlock.ReadFlags(); break;
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
    }

    public enum PlatformDataFlag
    {
        PlatformLeft = 0x00000002,
        PlatformRight = 0x00000004,
    }

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
        public int StaticDetailLevel;
        public uint StaticFlags;
        public uint VDbId;

        [NonSerialized]
        protected TokenID TokenID;
        
        public virtual void AddOrModifyObj(SBR subBlock)
        {
            
        }

        public void ReadBlock(SBR block)
        {
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    if (subBlock.ID == TokenID.UiD) UID = subBlock.ReadUInt();
                    else
                    {
                        AddOrModifyObj(subBlock);
                    }
                }
            }
        }

        public virtual void Serialize(StringBuilder sb)
        {
            var type = GetType().ToString();
            if (type == typeof(BaseObj).ToString()) type = "Gantry"; // FIXME

            sb.AppendLine();
            sb.Append("    " + TokenID + " (");
            foreach (var field in GetType().GetFields())
            {
                if (field.IsNotSerialized)
                    continue;

                var fieldValue = field.GetValue(this);
                if (fieldValue == null)
                    continue;

                if (fieldValue is ICollection)
                {
                    foreach (var listItem in fieldValue as IEnumerable)
                    {
                        sb.AppendLine();
                        sb.Append("        " + listItem.GetType().Name + " ( ");
                        listItem?.GetType().GetMethod("Serialize").Invoke(listItem, new object[] { sb });
                        sb.Append(" )");
                    }
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    sb.AppendLine();
                    sb.Append("        " + field.Name + " ( ");
                    fieldValue?.GetType().GetMethod("Serialize").Invoke(fieldValue, new object[] { sb });
                    sb.Append(" )");
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    sb.AppendLine();
                    var fieldVector3 = (Vector3)fieldValue;
                    sb.Append(fieldVector3.X + " " + fieldVector3.Y + " " + fieldVector3.Z);
                }
                else if (field.Name == TokenID.StaticFlags.ToString() || field.Name == TokenID.PlatformData.ToString())
                {
                    sb.AppendLine();
                    sb.Append("        " + field.Name + " ( " + ((uint)fieldValue).ToString("X8") + " ) ");
                }
                else
                {
                    sb.AppendLine();
                    sb.Append("        " + field.Name + " ( " + fieldValue + " ) ");
                }
            }
            sb.AppendLine();
            sb.Append("    )");
        }
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
        public readonly float AX, AY, AZ, BX, BY, BZ, CX, CY, CZ;

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

        public void Serialize(StringBuilder sb)
        {
            sb.Append(AX.ToString("F") + " " + AY.ToString("F") + " " + AZ.ToString("F") + " "
                + BX.ToString("F") + " " + BY.ToString("F") + " " + BZ.ToString("F") + " "
                + CX.ToString("F") + " " + CY.ToString("F") + " " + CZ.ToString("F"));
        }
    }

    public class JNodePosn
    {
        public readonly int TileX, TileZ;
        public readonly float X, Y, Z;

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

        public void Serialize(StringBuilder sb) { sb.Append(TileZ + " " + TileZ + " " + X.ToString("F") + " " + Y.ToString("F") + " " + Z.ToString("F")); }
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
            var slope = GetSlope();
            SetAngles(compassRad, slope);
        }

        public void SetBearing(float dx, float dz)
        {
            var slope = GetSlope();
            var compassRad = MstsUtility.AngleDxDz(dx, dz);
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
            var slope = GetSlope();
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
            var a1 = compassRad;
            var a2 = 0F;
            var a3 = tiltRad;

            var C1 = (float)Math.Cos(a1);
            var S1 = (float)Math.Sin(a1);
            var C2 = (float)Math.Cos(a2);
            var S2 = (float)Math.Sin(a2);
            var C3 = (float)Math.Cos(a3);
            var S3 = (float)Math.Sin(a3);

            var w = (float)Math.Sqrt(1.0 + C1 * C2 + C1 * C3 - S1 * S2 * S3 + C2 * C3) / 2.0f;
            float x, y, z;

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
            var compassAngleRad = MstsUtility.AngleDxDz(DX(), DZ());
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
            var x = -A; // imaginary i part of quaternion
            var y = -B; // imaginary j part of quaternion
            var z = -C; // imaginary k part of quaternion
            var w = D; // real part of quaternionfloat 

            //From http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
            //p2.x = ( w*w*p1.x + 2*y*w*p1.z - 2*z*w*p1.y + x*x*p1.x + 2*y*x*p1.y + 2*z*x*p1.z - z*z*p1.x - y*y*p1.x );	
            //p2.y = ( 2*x*y*p1.x + y*y*p1.y + 2*z*y*p1.z + 2*w*z*p1.x - z*z*p1.y + w*w*p1.y - 2*x*w*p1.z - x*x*p1.y );	
            //p2.z = ( 2*x*z*p1.x + 2*y*z*p1.y + z*z*p1.z - 2*w*y*p1.x - y*y*p1.z + 2*w*x*p1.y - x*x*p1.z + w*w*p1.z );

            var dy = (2 * z * y - 2 * x * w);
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

            var x = -A;
            var y = -B;
            var z = -C;
            var w = D;

            var dX = (2 * y * w + 2 * z * x);
            return dX;
        }

        public float DZ()
        {
            var x = -A;
            var y = -B;
            var z = -C;
            var w = D;

            return z * z - y * y - x * x + w * w;
        }

        public float GetSlope()   // Return the slope, +radians is tilted up
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            var qx = -A;
            var qy = -B;
            var qz = -C;
            var qw = D;

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

        public float GetBearing()   // Return the bearing
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            var qx = -A;
            var qy = -B;
            var qz = -C;
            var qw = D;

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
            var a1 = d1.GetBearing();
            var a2 = d2.GetBearing();

            var a = a1 - a2;

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

            var x = -A; // imaginary i part of quaternion
            var y = -B; // imaginary j part of quaternion
            var z = -C; // imaginary k part of quaternion
            var w = D; // real part of quaternionfloat 

            var p2 = new TWorldPosition();

            p2.X = (w * w * p1.X + 2 * y * w * p1.Z - 2 * z * w * p1.Y + x * x * p1.X + 2 * y * x * p1.Y + 2 * z * x * p1.Z - z * z * p1.X - y * y * p1.X);
            p2.Y = (2 * x * y * p1.X + y * y * p1.Y + 2 * z * y * p1.Z + 2 * w * z * p1.X - z * z * p1.Y + w * w * p1.Y - 2 * x * w * p1.Z - x * x * p1.Y);
            p2.Z = (2 * x * z * p1.X + 2 * y * z * p1.Y + z * z * p1.Z - 2 * w * y * p1.X - y * y * p1.Z + 2 * w * x * p1.Y - x * x * p1.Z + w * w * p1.Z);

            return p2;
        }

        public void Serialize(StringBuilder sb) { sb.Append(A.ToString("F") + " " + B.ToString("F") + " " + C.ToString("F") + " " + D.ToString("F")); }
    }

    public class TWorldPosition
    {
        public float X;
        public float Y;
        public float Z;

        public static readonly TWorldPosition Zero = new TWorldPosition();

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
            var DRight = new TWorldDirection(d);
            DRight.Rotate(MathHelper.ToRadians(90));
            Move(DRight, distanceRight);
        }

        public static float PointDistance(TWorldPosition p1, TWorldPosition p2)
        // distance between p1 and p2 along the surface
        {
            var dX = p1.X - p2.X;
            var dZ = p1.Z - p2.Z;
            return (float)Math.Sqrt(dX * dX + dZ * dZ);
        }

        public virtual void Serialize(StringBuilder sb) { sb.Append(X.ToString("F") + " " + Y.ToString("F") + " " + Z.ToString("F")); }
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
                using (var subBlock = block.ReadSubBlock())
                {
                    units.Add(new SignalUnit(subBlock));
                }
            }
            block.VerifyEndOfBlock();
            Units = units.ToArray();
        }

        public void Serialize(StringBuilder sb)
        {
            sb.Append(Units.Length);
            sb.AppendLine();
            foreach (var unit in Units)
            {
                unit.Serialize(sb);
            }
            sb.AppendLine();
            sb.Append("       ");
        }
    }

    public class SignalUnit
    {
        public readonly int SubObj;
        public readonly uint UnknownFunctionality;
        public readonly uint TrItem;

        public SignalUnit(SBR block)
        {
            block.VerifyID(TokenID.SignalUnit);
            SubObj = block.ReadInt();
            using (var subBlock = block.ReadSubBlock())
            {
                subBlock.VerifyID(TokenID.TrItemId);
                UnknownFunctionality = subBlock.ReadUInt();
                TrItem = subBlock.ReadUInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }

        public virtual void Serialize(StringBuilder sb)
        {
            sb.Append("            SignalUnit ( " + SubObj);
            sb.AppendLine();
            sb.Append("                TrItemId ( " + " " + UnknownFunctionality + " " + TrItem + " )");
            sb.AppendLine();
            sb.Append("            )");
        }
    }
}
