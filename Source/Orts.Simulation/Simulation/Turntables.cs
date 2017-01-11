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
    /// 


    public class TurntableFile
    {
        public TurntableFile(string filePath, string shapePath, List<Turntable> turntables, Simulator simulator)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var count = stf.ReadInt(null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("turntable", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra Turntable");
                        else
                            turntables.Add(new Turntable(stf, simulator));
                    }),
                });
                if (count > 0)
                    STFException.TraceWarning(stf, count + " missing Turntable(s)");
            }
        }
    }

    public class Turntable
    {
        // Fixed data
        public string WFile;
        public int UID;
        public float Diameter;
        public List<float> Angles = new List<float>();
        public List<string> Animations = new List<string>();
        public WorldPosition WorldPosition = new WorldPosition();
        public int TrackShapeIndex;
        public Vector3 CenterOffset; // shape offset of center of Turntable;
        public float StartingY = 0; // starting yaw angle
        protected int[] MyTrackNodesIndex;
        protected int[] MyTrVectorSectionsIndex;
        protected bool[] MyTrackNodesOrientation; // true if forward, false if backward;
        // Dynamic data
        public bool Continuous; // continuous motion on
        public bool Clockwise; // clockwise motion on
        public bool Counterclockwise; // counterclockwise motion on
        public float YAngle = 0; // Y angle of animated part, to be compared with Y angles of endpoints
        public bool ForwardConnected = true; // Platform has its forward part connected to a track
        public bool RearConnected = false; // Platform has its rear part connected to a track
        public bool SaveForwardConnected = true; // Platform has its forward part connected to a track
        public bool SaveRearConnected = false; // Platform has its rear part connected to a track
        public int ConnectedTrackEnd = 0; // 
        public int ForwardConnectedTarget = -1; // index of trackend connected
        public int RearConnectedTarget = -1; // index of trackend connected
        public bool GoToTarget = false;
        public float TargetY = 0; //final target for Viewer;
        public List<Matrix> RelativeCarPositions;
        public Vector3 RelativeFrontTravellerXNALocation;
        public Vector3 RelativeRearTravellerXNALocation;
        public Vector3 FinalFrontTravellerXNALocation;
        public Vector3 FinalRearTravellerXNALocation;
        public Matrix AnimationXNAMatrix = Matrix.Identity;
        public bool LastConnection = true; // true if Forward connected, false if Rear connected.
        public bool ConnectionToggled = false; // connection has toggled from last time, trainset must flip
        // additions to manage rotation of wagons
        public List<TrainOnTurntable> TrainsOnTurntable = new List<TrainOnTurntable>(); // List of trains on turntable


        public Signals signalRef { get; protected set; }
        public Simulator Simulator;

        public Turntable(STFReader stf, Simulator simulator)
        {
            Simulator = simulator;
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
                new STFReader.TokenProcessor("diameter", ()=>{ Diameter = stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("xoffset", ()=>{ CenterOffset.X = stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("zoffset", ()=>{ CenterOffset.Z = -stf.ReadFloatBlock(STFReader.UNITS.None , null);}),
                new STFReader.TokenProcessor("trackshapeindex", ()=>
                {
                    TrackShapeIndex = stf.ReadIntBlock(-1);
                    InitializeAnglesAndTrackNodes();
                }),
             });
        }

        /// <summary>
        /// Saves the general variable parameters
        /// Called from within the Simulator class.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
        outf.Write(Continuous); 
        outf.Write(Clockwise);
        outf.Write(Counterclockwise);
        outf.Write(YAngle);
        outf.Write(ForwardConnected);
        outf.Write(RearConnected);
        outf.Write(SaveForwardConnected);
        outf.Write(SaveRearConnected);
        outf.Write(ConnectedTrackEnd); 
        outf.Write(ForwardConnectedTarget);
        outf.Write(RearConnectedTarget);
        outf.Write(GoToTarget);
        outf.Write(TargetY);
        outf.Write(TrainsOnTurntable.Count);
        foreach (var trainOnTurntable in TrainsOnTurntable) trainOnTurntable.Save(outf);
 //       public List<Matrix> RelativeCarPositions;
        SaveVector(outf, RelativeFrontTravellerXNALocation);
        SaveVector(outf, RelativeRearTravellerXNALocation);
        SaveVector(outf, FinalFrontTravellerXNALocation);
        SaveVector(outf, FinalRearTravellerXNALocation);
//        public Matrix AnimationXNAMatrix = Matrix.Identity;
        outf.Write(LastConnection);
        outf.Write(ConnectionToggled);
        }

        private void SaveVector(BinaryWriter outf, Vector3 vector)
        {
            outf.Write(vector.X);
            outf.Write(vector.Y);
            outf.Write(vector.Z);
        }


        /// <summary>
        /// Restores the general variable parameters
        /// Called from within the Simulator class.
        /// </summary>
        public void Restore(BinaryReader inf, Simulator simulator)
        {
            Continuous = inf.ReadBoolean();
            Clockwise = inf.ReadBoolean();
            Counterclockwise = inf.ReadBoolean();
            YAngle = inf.ReadSingle();
            ForwardConnected = inf.ReadBoolean();
            RearConnected = inf.ReadBoolean();
            SaveForwardConnected = inf.ReadBoolean();
            SaveRearConnected = inf.ReadBoolean();
            ConnectedTrackEnd = inf.ReadInt32();
            ForwardConnectedTarget = inf.ReadInt32();
            RearConnectedTarget = inf.ReadInt32();
            GoToTarget = inf.ReadBoolean();
            TargetY = inf.ReadSingle();
            var trainsOnTurntable = inf.ReadInt32();
            while (trainsOnTurntable > 0)
            {
                TrainOnTurntable trainOnTurntable = new TrainOnTurntable(Simulator);
                trainOnTurntable.Restore(inf);
                trainsOnTurntable--;
            }

            RelativeFrontTravellerXNALocation = RestoreVector(inf);
            RelativeRearTravellerXNALocation = RestoreVector(inf);
            FinalFrontTravellerXNALocation = RestoreVector(inf);
            FinalRearTravellerXNALocation = RestoreVector(inf);
            LastConnection = inf.ReadBoolean();
            ConnectionToggled = inf.ReadBoolean();
        }

        private Vector3 RestoreVector(BinaryReader inf)
        {
            Vector3 vector;
            vector.X = inf.ReadSingle();
            vector.Y = inf.ReadSingle();
            vector.Z = inf.ReadSingle();
            return vector;
        }

        protected void InitializeAnglesAndTrackNodes()
        {
            var trackShape = Simulator.TSectionDat.TrackShapes.Get((uint)TrackShapeIndex);
            var nSections = Simulator.TSectionDat.TrackShapes[(uint)TrackShapeIndex].SectionIdxs[0].NoSections;
            MyTrackNodesIndex = new int[Simulator.TSectionDat.TrackShapes[(uint)TrackShapeIndex].SectionIdxs.Length];
            MyTrackNodesOrientation = new bool[MyTrackNodesIndex.Length];
            MyTrVectorSectionsIndex = new int[MyTrackNodesIndex.Length];
            var iMyTrackNodes = 0;
            foreach (var sectionIdx in trackShape.SectionIdxs)
            {
                Angles.Add(MathHelper.ToRadians((float)sectionIdx.A));
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
        }

        public void ReInitTrainPositions(Matrix animationXNAMatrix)
        {
            AnimationXNAMatrix = animationXNAMatrix;
            if (this == Simulator.ActiveTurntable && TrainsOnTurntable.Count == 1)
            {
                var train = TrainsOnTurntable[0].Train;
                if (TrainsOnTurntable[0].FrontOnBoard && TrainsOnTurntable[0].BackOnBoard && Math.Abs(train.SpeedMpS) < 0.1)
                {
                    var invAnimationXNAMatrix = Matrix.Invert(AnimationXNAMatrix);
                    RelativeCarPositions = new List<Matrix>();
                    foreach (TrainCar trainCar in train.Cars)
                    {
                        var relativeCarPosition = Matrix.Identity;
                        trainCar.WorldPosition.NormalizeTo(WorldPosition.TileX, WorldPosition.TileZ);
                        relativeCarPosition = Matrix.Multiply(trainCar.WorldPosition.XNAMatrix, invAnimationXNAMatrix);
                        RelativeCarPositions.Add(relativeCarPosition);
                    }

                }
            }
        }

        /// <summary>
        /// Computes the nearest turntable exit in the actual direction
        /// Returns the Y angle to be compared.
        /// </summary>
        public void ComputeTarget(bool isClockwise)
        {
            if (!Continuous) return;
            Continuous = false;
            GoToTarget = false;
            Clockwise = isClockwise;
            Counterclockwise = !isClockwise;
            if (Clockwise)
            {
                var forwardAngleDiff = 3.5f;
                var rearAngleDiff = 3.5f;
                ForwardConnected = false;
                RearConnected = false;
                if (Angles.Count <= 0)
                {
                    Clockwise = false;
                    ForwardConnectedTarget = -1;
                    RearConnectedTarget = -1;
                }
                else
                {
                    for (int iAngle = Angles.Count - 1; iAngle >= 0; iAngle--)
                    {
                        if (MyTrackNodesIndex[iAngle] != -1 && MyTrVectorSectionsIndex[iAngle] != -1)
                        {
                            var thisAngleDiff = MathHelper.WrapAngle(Angles[iAngle] + YAngle);
                            if (thisAngleDiff < forwardAngleDiff && thisAngleDiff >= 0)
                            {
                                ForwardConnectedTarget = iAngle;
                                forwardAngleDiff = thisAngleDiff;
                            }
                            thisAngleDiff = MathHelper.WrapAngle(Angles[iAngle] + YAngle + (float)Math.PI);
                            if (thisAngleDiff < rearAngleDiff && thisAngleDiff >= 0)
                            {
                                RearConnectedTarget = iAngle;
                                rearAngleDiff = thisAngleDiff;
                            }
                        }
                    }
                    if (forwardAngleDiff < 0.1 || rearAngleDiff < 0.1)
                    {
                        if (forwardAngleDiff < rearAngleDiff && Math.Abs(forwardAngleDiff - rearAngleDiff) > 0.01)
                        {
                            RearConnectedTarget = -1;
                        }
                        else if (forwardAngleDiff > rearAngleDiff && Math.Abs(forwardAngleDiff - rearAngleDiff) > 0.01)
                        {
                            ForwardConnectedTarget = -1;
                        }
                    }
                    else
                    {
                        Clockwise = false;
                        ForwardConnectedTarget = -1;
                        RearConnectedTarget = -1;
                    }
                }
            }
            else if (Counterclockwise)
            {
                var forwardAngleDiff = -3.5f;
                var rearAngleDiff = -3.5f;
                ForwardConnected = false;
                RearConnected = false;
                if (Angles.Count <= 0)
                {
                    Counterclockwise = false;
                    ForwardConnectedTarget = -1;
                    RearConnectedTarget = -1;
                }
                else
                {
                    for (int iAngle = 0; iAngle <= Angles.Count - 1; iAngle++)
                    {
                        if (MyTrackNodesIndex[iAngle] != -1 && MyTrVectorSectionsIndex[iAngle] != -1)
                        {
                            var thisAngleDiff = MathHelper.WrapAngle(Angles[iAngle] + YAngle);
                            if (thisAngleDiff > forwardAngleDiff && thisAngleDiff <= 0)
                            {
                                ForwardConnectedTarget = iAngle;
                                forwardAngleDiff = thisAngleDiff;
                            }
                            thisAngleDiff = MathHelper.WrapAngle(Angles[iAngle] + YAngle + (float)Math.PI);
                            if (thisAngleDiff > rearAngleDiff && thisAngleDiff <= 0)
                            {
                                RearConnectedTarget = iAngle;
                                rearAngleDiff = thisAngleDiff;
                            }
                        }
                    }
                    if (forwardAngleDiff > -0.1 || rearAngleDiff > -0.1)
                    {
                        if (forwardAngleDiff > rearAngleDiff && Math.Abs(forwardAngleDiff - rearAngleDiff) > 0.01)
                        {
                            RearConnectedTarget = -1;
                        }
                        else if (forwardAngleDiff < rearAngleDiff && Math.Abs(forwardAngleDiff - rearAngleDiff) > 0.01)
                        {
                            ForwardConnectedTarget = -1;
                        }
                    }
                    else
                    {
                        Counterclockwise = false;
                        ForwardConnectedTarget = -1;
                        RearConnectedTarget = -1;
                    }
                }

            }
            return;
        }

        /// <summary>
        /// Starts continuous movement
        /// 
        /// </summary>
        /// 
        public void StartContinuous(bool isClockwise)
        {
            if (TrainsOnTurntable.Count > 1 || (TrainsOnTurntable.Count == 1 && TrainsOnTurntable[0].FrontOnBoard ^ TrainsOnTurntable[0].BackOnBoard))
            {
                Clockwise = false;
                Counterclockwise = false;
                Continuous = false;
                Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Train partially on turntable, can't rotate"));
                return;
            }
            if (TrainsOnTurntable.Count == 1 && TrainsOnTurntable[0].FrontOnBoard && TrainsOnTurntable[0].BackOnBoard)
            {
                // Preparing for rotation
                var train = TrainsOnTurntable[0].Train;
                if (Math.Abs(train.SpeedMpS) > 0.1 || (train.LeadLocomotiveIndex != -1 && (train.LeadLocomotive.ThrottlePercent >= 1 || !(train.LeadLocomotive.Direction == Direction.N 
                 || Math.Abs(train.MUReverserPercent) <= 1))) || (train.ControlMode != Train.TRAIN_CONTROL.MANUAL && train.ControlMode != Train.TRAIN_CONTROL.TURNTABLE &&
                 train.ControlMode != Train.TRAIN_CONTROL.EXPLORER && train.ControlMode != Train.TRAIN_CONTROL.UNDEFINED))
                {
                    Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Rotation can't start: check throttle, speed, direction and control mode"));
                    return;
                }
                if (train.ControlMode == Train.TRAIN_CONTROL.MANUAL || train.ControlMode == Train.TRAIN_CONTROL.EXPLORER || train.ControlMode == Train.TRAIN_CONTROL.UNDEFINED)
                {
                    SaveForwardConnected = ForwardConnected ^ !MyTrackNodesOrientation[ConnectedTrackEnd];
                    SaveRearConnected = RearConnected;
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
                Simulator.Confirmer.Information (Simulator.Catalog.GetStringFmt("Turntable starting rotation with train"));
                // Computing position of cars relative to center of platform

             }
             Clockwise = isClockwise;
             Counterclockwise = !isClockwise;
             Continuous = true;
        }

        public void ComputeCenter(WorldPosition worldPosition)
        {
            Vector3 centerCoordinates;
            Vector3.Transform(ref CenterOffset, ref worldPosition.XNAMatrix, out centerCoordinates);
            WorldPosition = new WorldPosition(worldPosition);
            WorldPosition.XNAMatrix.M41 = centerCoordinates.X;
            WorldPosition.XNAMatrix.M42 = centerCoordinates.Y;
            WorldPosition.XNAMatrix.M43 = centerCoordinates.Z;
        }

        public void RotateTrain(Matrix animationXNAMatrix)
        {
            AnimationXNAMatrix = animationXNAMatrix;
            if ((Clockwise || Counterclockwise || GoToTarget) && TrainsOnTurntable.Count == 1 && TrainsOnTurntable[0].FrontOnBoard &&
                TrainsOnTurntable[0].BackOnBoard && TrainsOnTurntable[0].Train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                // Rotate together also train
                var iRelativeCarPositions = 0;
                foreach (TrainCar traincar in TrainsOnTurntable[0].Train.Cars)
                {
                    traincar.WorldPosition.XNAMatrix = Matrix.Multiply(RelativeCarPositions[iRelativeCarPositions], AnimationXNAMatrix);
                    iRelativeCarPositions++;
                }
            }
        }

        public void RecalculateTravellerXNALocations(Matrix animationXNAMatrix)
        {
            FinalFrontTravellerXNALocation = Vector3.Transform(RelativeFrontTravellerXNALocation, animationXNAMatrix);
            FinalRearTravellerXNALocation = Vector3.Transform(RelativeRearTravellerXNALocation, animationXNAMatrix);
        }

        public void Update()
        {
            foreach (var trainOnTurntable in TrainsOnTurntable)
                if (trainOnTurntable.FrontOnBoard ^ trainOnTurntable.BackOnBoard)
                {
                    Clockwise = false;
                    Counterclockwise = false;
                    Continuous = false;
                    return;
                }
            if (Continuous)
            {
                ForwardConnected = false;
                RearConnected = false;
                ConnectedTrackEnd = -1;
                GoToTarget = false;
            }
            else
            {
                if (Clockwise)
                {
                    ForwardConnected = false;
                    RearConnected = false;
                    if (ForwardConnectedTarget != -1)
                    {
                        if (Math.Abs(MathHelper.WrapAngle(Angles[ForwardConnectedTarget] + YAngle)) < 0.005)
                        {
                            ForwardConnected = true;
                            Clockwise = false;
                            ConnectedTrackEnd = ForwardConnectedTarget;
                            Simulator.Confirmer.Information (Simulator.Catalog.GetStringFmt("Turntable forward connected"));
                            GoToTarget = true;
                            TargetY = -Angles[ForwardConnectedTarget];
                            ConnectionToggled = LastConnection;
                            LastConnection = true;
                        }
                    }
                    else if (RearConnectedTarget != -1)
                    {
                        if (Math.Abs(MathHelper.WrapAngle(Angles[RearConnectedTarget] + YAngle + (float)Math.PI)) < 0.0055)
                        {
                            RearConnected = true;
                            Clockwise = false;
                            ConnectedTrackEnd = RearConnectedTarget;
                            Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Turntable backward connected"));
                            GoToTarget = true;
                            TargetY = -MathHelper.WrapAngle(Angles[RearConnectedTarget] + (float)Math.PI);
                            ConnectionToggled = !LastConnection;
                            LastConnection = false;
                        }
                    }
                }
                else if (Counterclockwise)
                {
                    ForwardConnected = false;
                    RearConnected = false;
                    if (ForwardConnectedTarget != -1)
                    {
                        if (Math.Abs(MathHelper.WrapAngle(Angles[ForwardConnectedTarget] + YAngle)) < 0.005)
                        {
                            ForwardConnected = true;
                            Counterclockwise = false;
                            ConnectedTrackEnd = ForwardConnectedTarget;
                            Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Turntable forward connected"));
                            GoToTarget = true;
                            TargetY = -Angles[ForwardConnectedTarget];
                            ConnectionToggled = LastConnection;
                            LastConnection = true;
                        }
                    }
                    else if (RearConnectedTarget != -1)
                    {
                        if (Math.Abs(MathHelper.WrapAngle(Angles[RearConnectedTarget] + YAngle + (float)Math.PI)) < 0.0055)
                        {
                            RearConnected = true;
                            Counterclockwise = false;
                            ConnectedTrackEnd = RearConnectedTarget;
                            Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Turntable backward connected"));
                            GoToTarget = true;
                            TargetY = -MathHelper.WrapAngle(Angles[RearConnectedTarget] + (float)Math.PI);
                            ConnectionToggled = !LastConnection;
                            LastConnection = false;
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
            Traveller.TravellerDirection direction = ForwardConnected ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward;
            direction = SaveForwardConnected ^ !MyTrackNodesOrientation[ConnectedTrackEnd]? direction : (direction == Traveller.TravellerDirection.Forward ? Traveller.TravellerDirection.Backward : Traveller.TravellerDirection.Forward);
            GoToTarget = false;
            if (TrainsOnTurntable.Count == 1)
            {
                var train = TrainsOnTurntable[0].Train;
                if (train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
                    train.ReenterTrackSections(MyTrackNodesIndex[ConnectedTrackEnd], MyTrVectorSectionsIndex[ConnectedTrackEnd], FinalFrontTravellerXNALocation, FinalRearTravellerXNALocation, direction);
            }
        }

        /// <summary>
        /// CheckTurntableAligned: checks if turntable aligned with entering train
        /// </summary>
        /// 

        private bool CheckTurntableAligned(Train train, bool forward)
        {
            Traveller.TravellerDirection direction;
            if ((ForwardConnected || RearConnected) && MyTrVectorSectionsIndex[ConnectedTrackEnd] != -1 && MyTrackNodesIndex[ConnectedTrackEnd] != -1 &&
                (MyTrackNodesIndex[ConnectedTrackEnd] == train.FrontTDBTraveller.TN.Index || MyTrackNodesIndex[ConnectedTrackEnd] == train.RearTDBTraveller.TN.Index))
            {
            direction = ForwardConnected ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward;
            return true;
            }
            direction = Traveller.TravellerDirection.Forward;
            return false;
        }

        /// <summary>
        /// CheckTrainOnTurntable: checks if actual player train is on turntable
        /// </summary>
        /// 
        public bool CheckTrainOnTurntable(Train train)
        {
            var trainIndex = TrainsOnTurntable.FindIndex(x => x.Train.Number == train.Number);
            if (WorldLocation.Within(train.FrontTDBTraveller.WorldLocation, WorldPosition.WorldLocation, Diameter/2))
            {
                if (trainIndex == -1 || !TrainsOnTurntable[trainIndex].FrontOnBoard)
                {
                    if (trainIndex == -1)
                    {
                        var trainOnTurntable = new TrainOnTurntable(train, Simulator);
                        trainIndex = TrainsOnTurntable.Count;
                        TrainsOnTurntable.Add(trainOnTurntable);
                    }
                    if (!TrainsOnTurntable[trainIndex].BackOnBoard)
                    {
                        // check if turntable aligned with train
                        var isAligned = CheckTurntableAligned(train, true);
                        if (!isAligned)
                        {
                            TrainsOnTurntable[trainIndex].SetFrontState(true);
                            Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Train slipped into non aligned turntable"));
                            train.SetTrainOutOfControl(Train.OUTOFCONTROL.SLIPPED_INTO_TURNTABLE);
                            train.SpeedMpS = 0;
                            foreach (var car in train.Cars) car.SpeedMpS = 0;
                            return false;
                        }
                     }                 
                    Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Train front on turntable"));
                }
                TrainsOnTurntable[trainIndex].SetFrontState(true);
            }
            else
            {
                if (trainIndex != -1 && TrainsOnTurntable[trainIndex].FrontOnBoard)
                {
                    Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Train front outside turntable"));
                    if (TrainsOnTurntable[trainIndex].BackOnBoard) TrainsOnTurntable[trainIndex].SetFrontState(false);
                    else
                    {
                        TrainsOnTurntable.RemoveAt(trainIndex);
                        trainIndex = -1;
                    }
                }
            }
            if (WorldLocation.Within(train.RearTDBTraveller.WorldLocation, WorldPosition.WorldLocation, Diameter / 2))
            {
                if (trainIndex == -1 || !TrainsOnTurntable[trainIndex].BackOnBoard)
                {
                    if (trainIndex == -1)
                    {
                        var trainOnTurntable = new TrainOnTurntable(train, Simulator);
                        trainIndex = TrainsOnTurntable.Count;
                        TrainsOnTurntable.Add(trainOnTurntable);
                    }
                    if (!TrainsOnTurntable[trainIndex].FrontOnBoard)
                    {
                        // check if turntable aligned with train
                        var isAligned = CheckTurntableAligned(train, false);
                        if (!isAligned)
                        {
                            TrainsOnTurntable[trainIndex].SetBackState(true);
                            Simulator.Confirmer.Warning(Simulator.Catalog.GetStringFmt("Train slipped into non aligned turntable"));
                            train.SetTrainOutOfControl(Train.OUTOFCONTROL.SLIPPED_INTO_TURNTABLE);
                            train.SpeedMpS = 0;
                            foreach (var car in train.Cars) car.SpeedMpS = 0;
                            return false;
                        }
                    }
                    Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Train rear on turntable"));
                }
                TrainsOnTurntable[trainIndex].SetBackState(true);
            }
            else
            {
                if (trainIndex != -1 && TrainsOnTurntable[trainIndex].BackOnBoard)
                {
                    Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Train rear outside turntable"));
                    if (TrainsOnTurntable[trainIndex].FrontOnBoard) TrainsOnTurntable[trainIndex].SetBackState(false);
                    else
                    {
                        TrainsOnTurntable.RemoveAt(trainIndex);
                        trainIndex = -1;
                    }
                }
            }
            if (Simulator.ActivityRun != null && !train.IsPathless && train.TrainType != Train.TRAINTYPE.STATIC && trainIndex != -1 && 
                TrainsOnTurntable[trainIndex].FrontOnBoard && TrainsOnTurntable[trainIndex].BackOnBoard && train.SpeedMpS <= 0.1f && train.ControlMode != Train.TRAIN_CONTROL.MANUAL &&
                train.TCRoute.activeSubpath == train.TCRoute.TCRouteSubpaths.Count - 1 && train.TCRoute.TCRouteSubpaths[train.TCRoute.activeSubpath].Count > 1 &&
                ( train.PresentPosition[0].RouteListIndex == train.TCRoute.TCRouteSubpaths[train.TCRoute.activeSubpath].Count - 2 ||
                train.PresentPosition[1].RouteListIndex == train.TCRoute.TCRouteSubpaths[train.TCRoute.activeSubpath].Count - 2))
                // Activity mode, train with path is at end of it and is being rotated on the turntable
            {
                train.IsPathless = true;
            }
            return false;
        }


        /// <summary>
        /// PerformUpdateActions: actions to be performed at every animation step
        /// </summary>
        /// 
        public void PerformUpdateActions ( Matrix absAnimationMatrix)
        {
            RotateTrain(absAnimationMatrix);
            if (GoToTarget && TrainsOnTurntable.Count == 1 && TrainsOnTurntable[0].Train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                RecalculateTravellerXNALocations(absAnimationMatrix);
            }
            if (GoToTarget) TargetExactlyReached();
        }
    }

    public class TrainOnTurntable
    {
        public Train Train;
        public bool FrontOnBoard;
        public bool BackOnBoard;
        public Simulator Simulator;

        public TrainOnTurntable (Train train, Simulator simulator)
        {
            Train = train;
            Simulator = simulator;
        }

        public TrainOnTurntable(Simulator simulator)
        {
            Simulator = simulator;
        }

        public void Save (BinaryWriter outf)
        {
            outf.Write(Simulator.Trains.IndexOf(Train));
            outf.Write(FrontOnBoard);
            outf.Write(BackOnBoard);
        }

        public void Restore(BinaryReader inf)
        {
            Train = Simulator.Trains[inf.ReadInt32()];
            FrontOnBoard = inf.ReadBoolean();
            BackOnBoard = inf.ReadBoolean();
        }

        public void SetFrontState (bool frontOnBoard)
        {
            FrontOnBoard = frontOnBoard;
        }

        public void SetBackState(bool backOnBoard)
        {
            BackOnBoard = backOnBoard;
        }

    }
}
