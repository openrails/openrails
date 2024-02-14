// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015, 2016 by the Open Rails project.
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
using Microsoft.Xna.Framework.Graphics;
using Orts.Parsers.Msts;
using ORTS.Common;
using Orts.Formats.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Simulation
{
    /// <summary>
    /// Reads file ORTSTurntables.dat and creates the instances of the turntables
    /// </summary>
    public class Transfertable : MovingTable
    {
        public float Span; // horizontal or vertical
        public List<float> Offsets = new List<float>();
        private bool VerticalTransfer = false;
        public float CenterOffsetComponent
        {
            get => VerticalTransfer ? CenterOffset.Y : CenterOffset.X;
        }
        public float OffsetDiff
        {
            get => RemotelyControlled ? 2.8f : 1.4f;
        }
        // Dynamic data
        public bool Forward; // forward motion on
        public bool Reverse; // reverse motion on
        public float OffsetPos = 0; // Offset Position of animated part, to be compared with offset positions of endpoints
        public bool Connected = true; // Transfertable is connected to a track
        public bool SaveConnected = true; // Transfertable is connected to a track
        public int ConnectedTarget = -1; // index of trackend connected
        public float TargetOffset = 0; //final target for Viewer;

        public Signals signalRef { get; protected set; }

        public Transfertable(STFReader stf, Simulator simulator): base(stf, simulator)
        {
            signalRef = Simulator.Signals;
            string animation;
            WorldPosition.XNAMatrix.M44 = 100000000; //WorlPosition not yet defined, will be loaded when loading related tile
            stf.MustMatch("(");
              stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("wfile", ()=>{
                    WFile = stf.ReadStringBlock(null);
                    WorldPosition.TileX = int.Parse(WFile.Substring(1, 7));
                    WorldPosition.TileZ = int.Parse(WFile.Substring(8, 7));                
                }),
                new STFReader.TokenProcessor("uid", ()=>{ UID = stf.ReadIntBlock(-1); }),
                new STFReader.TokenProcessor("animation", ()=>{ animation = stf.ReadStringBlock(null);
                                                                Animations.Add(animation.ToLower());}),
                new STFReader.TokenProcessor("verticaltransfer", ()=>{ VerticalTransfer = stf.ReadBoolBlock(false);}),
                new STFReader.TokenProcessor("length", ()=>{ Length = stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("xoffset", ()=>{ CenterOffset.X = stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("zoffset", ()=>{ CenterOffset.Z = -stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("yoffset", ()=>{ CenterOffset.Y = stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("trackshapeindex", ()=>
                {
                    TrackShapeIndex = stf.ReadIntBlock(-1);
                    InitializeOffsetsAndTrackNodes();
                }),
             });
        }

        /// <summary>
        /// Saves the general variable parameters
        /// Called from within the Simulator class.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Forward);
            outf.Write(Reverse);
            outf.Write(OffsetPos);
            outf.Write(Connected);
            outf.Write(SaveConnected);
            outf.Write(ConnectedTarget);
            outf.Write(TargetOffset);
        }

        /// <summary>
        /// Restores the general variable parameters
        /// Called from within the Simulator class.
        /// </summary>
        public override void Restore(BinaryReader inf, Simulator simulator)
        {
            base.Restore(inf, simulator);
            Forward = inf.ReadBoolean();
            Reverse = inf.ReadBoolean();
            OffsetPos = inf.ReadSingle();
            Connected = inf.ReadBoolean();
            SaveConnected = inf.ReadBoolean();
            ConnectedTarget = inf.ReadInt32();
            TargetOffset = inf.ReadSingle();
        }

        protected void InitializeOffsetsAndTrackNodes()
        {
            var trackShape = Simulator.TSectionDat.TrackShapes.Get((uint)TrackShapeIndex);
            var nSections = trackShape.SectionIdxs[0].NoSections;
            MyTrackNodesIndex = new int[trackShape.SectionIdxs.Length];
            MyTrackNodesOrientation = new bool[MyTrackNodesIndex.Length];
            MyTrVectorSectionsIndex = new int[MyTrackNodesIndex.Length];
            var iMyTrackNodes = 0;
            foreach (var sectionIdx in trackShape.SectionIdxs)
            {
                Offsets.Add(VerticalTransfer ? (float)sectionIdx.Y : (float)sectionIdx.X);
                MyTrackNodesIndex[iMyTrackNodes] = -1;
                MyTrVectorSectionsIndex[iMyTrackNodes] = -1;
                iMyTrackNodes++;
            }
            var trackNodes = Simulator.TDB.TrackDB.TrackNodes;
            int iTrackNode = 0;
            for (iTrackNode = 1; iTrackNode < trackNodes.Length; iTrackNode++)
            {
                if (trackNodes[iTrackNode].TrVectorNode != null && trackNodes[iTrackNode].TrVectorNode.TrVectorSections != null)
                {
                    var iTrVectorSection = Array.FindIndex(trackNodes[iTrackNode].TrVectorNode.TrVectorSections, trVectorSection =>
                        (trVectorSection.WFNameX == WorldPosition.TileX && trVectorSection.WFNameZ == WorldPosition.TileZ && trVectorSection.WorldFileUiD == UID));
                    if (iTrVectorSection >= 0)
                    {
                        if (trackNodes[iTrackNode].TrVectorNode.TrVectorSections.Length > (int)nSections)
                        {
                            iMyTrackNodes = trackNodes[iTrackNode].TrVectorNode.TrVectorSections[iTrVectorSection].Flag1 / 2;
                            MyTrackNodesIndex[iMyTrackNodes] = iTrackNode;
                            MyTrVectorSectionsIndex[iMyTrackNodes] = iTrVectorSection;
                            MyTrackNodesOrientation[iMyTrackNodes] = trackNodes[iTrackNode].TrVectorNode.TrVectorSections[iTrVectorSection].Flag1 % 2 == 0 ? true : false;
                        }
                    }
                }
            }
            if (VerticalTransfer)
            {
                OffsetPos = CenterOffset.Y;
                Span = (float)(trackShape.SectionIdxs[trackShape.SectionIdxs.Length - 1].Y - trackShape.SectionIdxs[0].Y);
            }
            else
            {
                OffsetPos = CenterOffset.X;
                Span = (float)(trackShape.SectionIdxs[trackShape.SectionIdxs.Length - 1].X - trackShape.SectionIdxs[0].X);
            }
        }

        /// <summary>
        /// Computes the nearest transfertable exit in the actual direction
        /// Returns the Y angle to be compared.
        /// </summary>
        public override void ComputeTarget(bool isForward)
        {
            if (!Continuous) return;
            if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                SubMessCode = SubMessageCode.GoToTarget;
                MultiPlayer.MPManager.Notify(new MultiPlayer.MSGMovingTbl(Simulator.ActiveMovingTableIndex, Orts.MultiPlayer.MPManager.GetUserName(), SubMessCode, isForward, OffsetPos).ToString());
            }
            RemotelyControlled = false;
            GeneralComputeTarget(isForward);
        }

        public void GeneralComputeTarget(bool isForward)
        {
            if (!Continuous) return;
            Continuous = false;
            GoToTarget = false;
            Forward = isForward;
            Reverse = !isForward;
            if (Forward)
            {
                Connected = false;
                if (Offsets.Count <= 0)
                {
                    Forward = false;
                    ConnectedTarget = -1;
                }
                else
                {
                    for (int iOffset = Offsets.Count - 1; iOffset >= 0; iOffset--)
                    {
                        if (MyTrackNodesIndex[iOffset] != -1 && MyTrVectorSectionsIndex[iOffset] != -1)
                        {
                            var thisOffsetDiff = Offsets[iOffset] - OffsetPos;
                            if (thisOffsetDiff < OffsetDiff && thisOffsetDiff >= 0)
                            {
                                ConnectedTarget = iOffset;
                                break;
                            }
                            else if (thisOffsetDiff < 0)
                            {
                                Forward = false;
                                ConnectedTarget = -1;
                                break;
                            }
                        }
                    }
                }
            }
            else if (Reverse)
            {
                Connected = false;
                if (Offsets.Count <= 0)
                {
                    Reverse = false;
                    ConnectedTarget = -1;
                }
                else
                {
                    for (int iOffset = 0; iOffset <= Offsets.Count - 1; iOffset++)
                    {
                        if (MyTrackNodesIndex[iOffset] != -1 && MyTrVectorSectionsIndex[iOffset] != -1)
                        {
                            var thisOffsetDiff = Offsets[iOffset] - OffsetPos;
                            if (thisOffsetDiff > -OffsetDiff && thisOffsetDiff <= 0)
                            {
                                ConnectedTarget = iOffset;
                                break;
                            }
                            else if (thisOffsetDiff > 0)
                            {
                                Reverse = false;
                                ConnectedTarget = -1;
                                break;
                            }
                        }
                    }
                }
            }
            RemotelyControlled = false;
            return;
        }

        /// <summary>
        /// Starts continuous movement
        /// 
        /// </summary>
        /// 
        public override void StartContinuous(bool isForward)
        {
            if (TrainsOnMovingTable.Count == 1 && TrainsOnMovingTable[0].FrontOnBoard && TrainsOnMovingTable[0].BackOnBoard)
            {
                // Preparing for rotation
                var train = TrainsOnMovingTable[0].Train;
                if (Math.Abs(train.SpeedMpS) > 0.1 || (train.LeadLocomotiveIndex != -1 && (train.LeadLocomotive.ThrottlePercent >= 1 || train.TrainType != Train.TRAINTYPE.REMOTE && !(train.LeadLocomotive.Direction == Direction.N
                 || Math.Abs(train.MUReverserPercent) <= 1))) || (train.ControlMode != Train.TRAIN_CONTROL.MANUAL && train.ControlMode != Train.TRAIN_CONTROL.TURNTABLE &&
                 train.ControlMode != Train.TRAIN_CONTROL.EXPLORER && train.ControlMode != Train.TRAIN_CONTROL.UNDEFINED))
                {
                    if (SendNotifications) Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Rotation can't start: check throttle, speed, direction and control mode"));
                    return;
                }
            }
            if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                SubMessCode = SubMessageCode.StartingContinuous;
                MultiPlayer.MPManager.Notify(new MultiPlayer.MSGMovingTbl(Simulator.ActiveMovingTableIndex, Orts.MultiPlayer.MPManager.GetUserName(), SubMessCode, isForward, OffsetPos).ToString());
            }
            GeneralStartContinuous(isForward);
        }

        public void GeneralStartContinuous(bool isForward)
        {
            if (TrainsOnMovingTable.Count > 1 || (TrainsOnMovingTable.Count == 1 && TrainsOnMovingTable[0].FrontOnBoard ^ TrainsOnMovingTable[0].BackOnBoard))
            {
                Forward = false;
                Reverse = false;
                Continuous = false;
                Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Train partially on transfertable, can't transfer"));
                return;
            }
            if (TrainsOnMovingTable.Count == 1 && TrainsOnMovingTable[0].FrontOnBoard && TrainsOnMovingTable[0].BackOnBoard)
            {
                // Preparing for transfer
                var train = TrainsOnMovingTable[0].Train;
                if (train.ControlMode == Train.TRAIN_CONTROL.MANUAL || train.ControlMode == Train.TRAIN_CONTROL.EXPLORER || train.ControlMode == Train.TRAIN_CONTROL.UNDEFINED)
                {
                    SaveConnected = Connected ^ !MyTrackNodesOrientation[ConnectedTrackEnd];
                    var invAnimationXNAMatrix = Matrix.Invert(AnimationXNAMatrix);
                    RelativeCarPositions = new List<Matrix>();
                    foreach (TrainCar trainCar in train.Cars)
                    {
                        var relativeCarPosition = Matrix.Identity;
                        trainCar.WorldPosition.NormalizeTo(WorldPosition.TileX, WorldPosition.TileZ);
                        relativeCarPosition = Matrix.Multiply(trainCar.WorldPosition.XNAMatrix, invAnimationXNAMatrix);
                        RelativeCarPositions.Add(relativeCarPosition);
                    }
                    var XNALocation = train.FrontTDBTraveller.Location;
                    XNALocation.Z = -XNALocation.Z;
                    XNALocation.X = XNALocation.X + 2048 * (train.FrontTDBTraveller.TileX - WorldPosition.TileX);
                    XNALocation.Z = XNALocation.Z - 2048 * (train.FrontTDBTraveller.TileZ - WorldPosition.TileZ);
                    RelativeFrontTravellerXNALocation = Vector3.Transform(XNALocation, invAnimationXNAMatrix);
                    XNALocation = train.RearTDBTraveller.Location;
                    XNALocation.Z = -XNALocation.Z;
                    XNALocation.X = XNALocation.X + 2048 * (train.RearTDBTraveller.TileX - WorldPosition.TileX);
                    XNALocation.Z = XNALocation.Z - 2048 * (train.RearTDBTraveller.TileZ - WorldPosition.TileZ);
                    RelativeRearTravellerXNALocation = Vector3.Transform(XNALocation, invAnimationXNAMatrix);
                    train.ControlMode = Train.TRAIN_CONTROL.TURNTABLE;
                }
                Simulator.Confirmer.Information (Simulator.Catalog.GetStringFmt("Transfertable starting transferring train"));
                // Computing position of cars relative to center of transfertable
             }
             Forward = isForward;
             Reverse = !isForward;
             Continuous = true;
        }

        public void ComputeCenter(WorldPosition worldPosition)
        {
            Vector3 movingCenterOffset = CenterOffset;
            if (VerticalTransfer)
                movingCenterOffset.Y = OffsetPos;
            else
                movingCenterOffset.X = OffsetPos;
            Vector3 originCoordinates;
            Vector3.Transform(ref movingCenterOffset, ref worldPosition.XNAMatrix, out originCoordinates);
            WorldPosition = new WorldPosition(worldPosition);
            WorldPosition.XNAMatrix.M41 = originCoordinates.X;
            WorldPosition.XNAMatrix.M42 = originCoordinates.Y;
            WorldPosition.XNAMatrix.M43 = originCoordinates.Z;
        }

        public void TransferTrain(Matrix animationXNAMatrix)
        {
            AnimationXNAMatrix = animationXNAMatrix;
            if ((Forward || Reverse || GoToTarget) && TrainsOnMovingTable.Count == 1 && TrainsOnMovingTable[0].FrontOnBoard &&
                TrainsOnMovingTable[0].BackOnBoard && TrainsOnMovingTable[0].Train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                // Move together also train
                var iRelativeCarPositions = 0;
                foreach (TrainCar traincar in TrainsOnMovingTable[0].Train.Cars)
                {
                    traincar.WorldPosition.XNAMatrix = Matrix.Multiply(RelativeCarPositions[iRelativeCarPositions], AnimationXNAMatrix);
                    traincar.UpdateFreightAnimationDiscretePositions();
                    iRelativeCarPositions++;
                }
            }
        }

        public override void Update()
        {
            foreach (var trainOnMovingTable in TrainsOnMovingTable)
                if (trainOnMovingTable.FrontOnBoard ^ trainOnMovingTable.BackOnBoard)
                {
                    Forward = false;
                    Reverse = false;
                    Continuous = false;
                    return;
                }
            if (Continuous)
            {
                Connected = false;
                ConnectedTrackEnd = -1;
                GoToTarget = false;
            }
            else
            {
                if (Forward)
                {
                    Connected = false;
                    if (ConnectedTarget != -1)
                    {
                        if (Offsets[ConnectedTarget] - OffsetPos < 0.005)
                        {
                            Connected = true;
                            Forward = false;
                            ConnectedTrackEnd = ConnectedTarget;
                            Simulator.Confirmer.Information (Simulator.Catalog.GetStringFmt("Transfertable connected"));
                            GoToTarget = true;
                            TargetOffset = Offsets[ConnectedTarget];
                        }
                    }
                 }
                else if (Reverse)
                {
                    Connected = false;
                    if (ConnectedTarget != -1)
                    {
                        if (OffsetPos - Offsets[ConnectedTarget] < 0.005)
                        {
                            Connected = true;
                            Reverse = false;
                            ConnectedTrackEnd = ConnectedTarget;
                            Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Transfertable connected"));
                            GoToTarget = true;
                            TargetOffset = Offsets[ConnectedTarget];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// TargetExactlyReached: if train on board, it can exit the turntable
        /// </summary>
        /// 
        public void TargetExactlyReached()
        {
            Traveller.TravellerDirection direction = Traveller.TravellerDirection.Forward;
            direction = SaveConnected ^ !MyTrackNodesOrientation[ConnectedTrackEnd]? direction : (direction == Traveller.TravellerDirection.Forward ? Traveller.TravellerDirection.Backward : Traveller.TravellerDirection.Forward);
            GoToTarget = false;
            if (TrainsOnMovingTable.Count == 1)
            {
                var train = TrainsOnMovingTable[0].Train;
                if (train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
                    train.ReenterTrackSections(MyTrackNodesIndex[ConnectedTrackEnd], MyTrVectorSectionsIndex[ConnectedTrackEnd], FinalFrontTravellerXNALocation, FinalRearTravellerXNALocation, direction);
            }
        }

        /// <summary>
        /// CheckMovingTableAligned: checks if transfertable aligned with entering train
        /// </summary>
        /// 
        public override bool CheckMovingTableAligned(Train train, bool forward)
        {
            if ((Connected) && MyTrVectorSectionsIndex[ConnectedTrackEnd] != -1 && MyTrackNodesIndex[ConnectedTrackEnd] != -1 &&
                (MyTrackNodesIndex[ConnectedTrackEnd] == train.FrontTDBTraveller.TN.Index || MyTrackNodesIndex[ConnectedTrackEnd] == train.RearTDBTraveller.TN.Index))
            {
            return true;
            }
            return false;
        }

        /// <summary>
        /// PerformUpdateActions: actions to be performed at every animation step
        /// </summary>
        /// 
        public void PerformUpdateActions ( Matrix absAnimationMatrix, WorldPosition worldPosition )
        {
            TransferTrain(absAnimationMatrix);
            if (GoToTarget && TrainsOnMovingTable.Count == 1 && TrainsOnMovingTable[0].Train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                RecalculateTravellerXNALocations(absAnimationMatrix);
            }
            if (GoToTarget)
            {
                ComputeCenter(worldPosition);
                TargetExactlyReached();
            }
        }
    }
}
