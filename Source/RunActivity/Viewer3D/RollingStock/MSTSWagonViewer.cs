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

// This file is the responsibility of the 3D & Environment Team.

// Debug for Sound Variables
//#define DEBUG_WHEEL_ANIMATION 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Viewer3D.RollingStock.SubSystems;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSWagonViewer : TrainCarViewer
    {
        public PoseableShape TrainCarShape { get; protected set; }
        protected AnimatedShape FreightShape;
        protected AnimatedShape InteriorShape;
        AnimatedShape FrontCouplerShape;
        AnimatedShape FrontCouplerOpenShape;
        AnimatedShape RearCouplerShape;
        AnimatedShape RearCouplerOpenShape;

        protected AnimatedShape FrontAirHoseShape;
        protected AnimatedShape FrontAirHoseDisconnectedShape;
        protected AnimatedShape RearAirHoseShape;
        protected AnimatedShape RearAirHoseDisconnectedShape;

        public static readonly Action Noop = () => { };
        /// <summary>
        /// Dictionary of built-in locomotive control keyboard commands, Action[] is in the order {KeyRelease, KeyPress}
        /// </summary>
        public Dictionary<UserCommand, Action[]> UserInputCommands = new Dictionary<UserCommand, Action[]>();

        // Wheels are rotated by hand instead of in the shape file.
        float WheelRotationR;
        Dictionary<int,List<int>> WheelPartIndexes = new Dictionary<int,List<int>>(); // List of wheels attached to each axle

        // Everything else is animated through the shape file.
        Dictionary<int,AnimatedPart> RunningGears; // List of animated parts linked to every axle
        AnimatedPart Pantograph1;
        AnimatedPart Pantograph2;
        AnimatedPart Pantograph3;
        AnimatedPart Pantograph4;
        AnimatedPart LeftDoor;
        AnimatedPart RightDoor;
        AnimatedPart Mirrors;
        protected AnimatedPart LeftWindowFront;
        protected AnimatedPart RightWindowFront;
        protected AnimatedPart LeftWindowRear;
        protected AnimatedPart RightWindowRear;
        protected AnimatedPart Wipers;
        protected AnimatedPart Bell;
        protected AnimatedPart Item1Continuous;
        protected AnimatedPart Item2Continuous;
        protected AnimatedPart Item1TwoState;
        protected AnimatedPart Item2TwoState;
        protected AnimatedPart BrakeCylinders;
        protected AnimatedPart Handbrakes;
        protected AnimatedPart BrakeRigging;
        AnimatedPart UnloadingParts;

        public Dictionary<string, List<ParticleEmitterViewer>> ParticleDrawers = new Dictionary<string, List<ParticleEmitterViewer>>();

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon)Car; } }


        // Create viewers for special steam/smoke effects on car
        List<ParticleEmitterViewer> HeatingHose = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingCompartmentSteamTrap = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingMainPipeSteamTrap = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WaterScoop = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WaterScoopReverse = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> TenderWaterOverflow = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WagonSmoke = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingSteamBoiler = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> BearingHotBox = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SteamBrake = new List<ParticleEmitterViewer>();

        // Create viewers for special steam effects on car
        List<ParticleEmitterViewer> WagonGenerator = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> DieselLocoGenerator = new List<ParticleEmitterViewer>();

        bool HasFirstPanto;
        int numBogie1, numBogie2, bogie1Axles, bogie2Axles = 0;
        int bogieMatrix1, bogieMatrix2 = 0;
        FreightAnimationsViewer FreightAnimations;

        public MSTSWagonViewer(Viewer viewer, MSTSWagon car)
            : base(viewer, car)
        {
            
            string steamTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\smokemain.ace";
            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";

            // Particle Drawers called in Wagon so that wagons can also have steam effects.
            ParticleDrawers = (
                from effect in MSTSWagon.EffectData
                select new KeyValuePair<string, List<ParticleEmitterViewer>>(effect.Key, new List<ParticleEmitterViewer>(
                    from data in effect.Value
                    select new ParticleEmitterViewer(viewer, data, car.WorldPosition)))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Initaialise particle viewers for special steam effects
            foreach (var emitter in ParticleDrawers)
            {

                // Exhaust for steam heating boiler
                if (emitter.Key.ToLowerInvariant() == "heatingsteamboilerfx")
                {
                    HeatingSteamBoiler.AddRange(emitter.Value);
                    // set flag to indicate that heating boiler is active on this car only - only sets first boiler steam effect found in the train
                    if (!car.IsTrainHeatingBoilerInitialised && !car.HeatingBoilerSet)
                    {
                        car.HeatingBoilerSet = true;
                        car.IsTrainHeatingBoilerInitialised = true;
                    }
                }

                foreach (var drawer in HeatingSteamBoiler)
                {
                    drawer.Initialize(dieselTexture);
                }

                // Exhaust for HEP/Power Generator
                if (emitter.Key.ToLowerInvariant() == "wagongeneratorfx")
                    WagonGenerator.AddRange(emitter.Value);
                
                foreach (var drawer in WagonGenerator)
                {
                    drawer.Initialize(dieselTexture);
                }

                // Smoke for wood/coal fire
                if (emitter.Key.ToLowerInvariant() == "wagonsmokefx")
                    WagonSmoke.AddRange(emitter.Value);

                foreach (var drawer in WagonSmoke)
                {
                    drawer.Initialize(steamTexture);
                }

                // Smoke for bearing hot box
                if (emitter.Key.ToLowerInvariant() == "bearinghotboxfx")
                    BearingHotBox.AddRange(emitter.Value);

                foreach (var drawer in BearingHotBox)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating hose 

                if (emitter.Key.ToLowerInvariant() == "heatinghosefx")
                    HeatingHose.AddRange(emitter.Value);

                foreach (var drawer in HeatingHose)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating compartment steam trap

                if (emitter.Key.ToLowerInvariant() == "heatingcompartmentsteamtrapfx")
                    HeatingCompartmentSteamTrap.AddRange(emitter.Value);

                foreach (var drawer in HeatingCompartmentSteamTrap)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating steam trap

                if (emitter.Key.ToLowerInvariant() == "heatingmainpipesteamtrapfx")
                    HeatingMainPipeSteamTrap.AddRange(emitter.Value);

                foreach (var drawer in HeatingMainPipeSteamTrap)
                {
                    drawer.Initialize(steamTexture);
                }

                // Water spray for when water scoop is in use (use steam effects for the time being) 
                // Forward motion
                if (emitter.Key.ToLowerInvariant() == "waterscoopfx")
                    WaterScoop.AddRange(emitter.Value);

                foreach (var drawer in WaterScoop)
                {
                    drawer.Initialize(steamTexture);
                }

                // Reverse motion

                if (emitter.Key.ToLowerInvariant() == "waterscoopreversefx")
                    WaterScoopReverse.AddRange(emitter.Value);

                foreach (var drawer in WaterScoopReverse)
                {
                    drawer.Initialize(steamTexture);
                }

                // Water overflow when tender is over full during water trough filling (use steam effects for the time being) 

                if (emitter.Key.ToLowerInvariant() == "tenderwateroverflowfx")
                    TenderWaterOverflow.AddRange(emitter.Value);

                foreach (var drawer in TenderWaterOverflow)
                {
                    drawer.Initialize(steamTexture);
                }

                if (emitter.Key.ToLowerInvariant() == "steambrakefx")
                    SteamBrake.AddRange(emitter.Value);

                foreach (var drawer in SteamBrake)
                {
                    drawer.Initialize(steamTexture);
                }

            }

            var wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";

            TrainCarShape = car.MainShapeFileName != string.Empty
                ? new PoseableShape(viewer, wagonFolderSlash + car.MainShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster)
                : new PoseableShape(viewer, null, car.WorldPosition);

            // This section initializes the MSTS style freight animation - can either be for a coal load, which will adjust with usage, or a static animation, such as additional shape.
            if (car.FreightShapeFileName != null)
            {
                car.HasFreightAnim = true;
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);

                // Reproducing MSTS "bug" of not allowing tender animation in case both minLevel and maxLevel are 0 or maxLevel <  minLevel 
                // Applies to both a standard tender locomotive or a tank locomotive (where coal load is on same "wagon" as the locomotive -  for the coal load on a tender or tank locomotive - in operation it will raise or lower with caol usage

                if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSSteamLocomotive)
                {

                    var NonTenderSteamLocomotive = MSTSWagon as MSTSSteamLocomotive;

                    if ((MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSLocomotive && (MSTSWagon.EngineType == TrainCar.EngineTypes.Steam && NonTenderSteamLocomotive.IsTenderRequired == 0.0)) && MSTSWagon.FreightAnimMaxLevelM != 0 && MSTSWagon.FreightAnimFlag > 0 && MSTSWagon.FreightAnimMaxLevelM > MSTSWagon.FreightAnimMinLevelM)
                    {
                        // Force allowing animation:
                        if (FreightShape.SharedShape.LodControls.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy.Length > 0)
                            FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] = 1;
                    }
                }
            }

            // Initialise Coupler shapes 
            if (car.FrontCoupler.Closed.ShapeFileName != null)
            {
                FrontCouplerShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontCoupler.Closed.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.FrontCoupler.Open.ShapeFileName != null)
            {
                FrontCouplerOpenShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontCoupler.Open.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearCoupler.Closed.ShapeFileName != null)
            {
                RearCouplerShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearCoupler.Closed.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearCoupler.Open.ShapeFileName != null)
            {
                RearCouplerOpenShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearCoupler.Open.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            // Initialise air hose shapes

            if (car.FrontAirHose.Connected.ShapeFileName != null)
            {
                FrontAirHoseShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontAirHose.Connected.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.FrontAirHose.Disconnected.ShapeFileName != null)
            {
                FrontAirHoseDisconnectedShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontAirHose.Disconnected.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearAirHose.Connected.ShapeFileName != null)
            {
                RearAirHoseShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearAirHose.Connected.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearAirHose.Disconnected.ShapeFileName != null)
            {
                RearAirHoseDisconnectedShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearAirHose.Disconnected.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }


            if (car.InteriorShapeFileName != null)
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.Interior, 30.0f);

            RunningGears = new Dictionary<int,AnimatedPart>();
            for (int i=-1; i<car.LocomotiveAxles.Count; i++)
            {
                RunningGears[i] = new AnimatedPart(TrainCarShape);
                WheelPartIndexes[i] = new List<int>();
            }
            Pantograph1 = new AnimatedPart(TrainCarShape);
            Pantograph2 = new AnimatedPart(TrainCarShape);
            Pantograph3 = new AnimatedPart(TrainCarShape);
            Pantograph4 = new AnimatedPart(TrainCarShape);
            LeftDoor = new AnimatedPart(TrainCarShape);
            RightDoor = new AnimatedPart(TrainCarShape);
            Mirrors = new AnimatedPart(TrainCarShape);
            LeftWindowFront = new AnimatedPart(TrainCarShape);
            RightWindowFront = new AnimatedPart(TrainCarShape);
            LeftWindowRear = new AnimatedPart(TrainCarShape);
            RightWindowRear = new AnimatedPart(TrainCarShape);
            Wipers = new AnimatedPart(TrainCarShape);
            UnloadingParts = new AnimatedPart(TrainCarShape);
            Bell = new AnimatedPart(TrainCarShape);
            Item1Continuous = new AnimatedPart(TrainCarShape);
            Item2Continuous = new AnimatedPart(TrainCarShape);
            Item1TwoState = new AnimatedPart(TrainCarShape);
            Item2TwoState = new AnimatedPart(TrainCarShape);
            BrakeCylinders = new AnimatedPart(TrainCarShape);
            Handbrakes = new AnimatedPart(TrainCarShape);
            BrakeRigging = new AnimatedPart(TrainCarShape);

            if (car.FreightAnimations != null)
                FreightAnimations = new FreightAnimationsViewer(viewer, car, wagonFolderSlash);

            LoadCarSounds(wagonFolderSlash);
            //if (!(MSTSWagon is MSTSLocomotive))
            //    LoadTrackSounds();
            Viewer.SoundProcess.AddSoundSource(this, new TrackSoundSource(MSTSWagon, Viewer));

            // Determine if it has first pantograph. So we can match unnamed panto parts correctly
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains('1'))
                {
                    if (TrainCarShape.SharedShape.MatrixNames[i].ToUpper().StartsWith("PANTO")) { HasFirstPanto = true; break; }
                }

            // Check bogies and wheels to find out what we have.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
            {
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE1"))
                {
                    bogieMatrix1 = i;
                    numBogie1 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE2"))
                {
                    bogieMatrix2 = i;
                    numBogie2 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE"))
                {
                    bogieMatrix1 = i;
                }
                // For now, the total axle count consisting of axles that are part of the bogie are being counted.
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS"))
                    if (TrainCarShape.SharedShape.MatrixNames[i].Length == 8)
                    {
                        var tpmatrix = TrainCarShape.SharedShape.GetParentMatrix(i);
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS22") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;

                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                    }
            }

            // Match up all the matrices with their parts.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.Hierarchy[i] == -1)
                    MatchMatrixToPart(car, i, 0);

            // Precompute bogie positioning parameters for later
            if (car.Parts.Count > 1)
            {
                car.BogieZOffsets = new float[car.Parts.Count - 1];

                float o = -car.CarLengthM / 2 - car.CentreOfGravityM.Z;
                float tempHeight = 0;
                for (int p = 1; p < car.Parts.Count; p++)
                {
                    car.BogieZOffsets[p - 1] = car.Parts[p].OffsetM.Z - o;
                    o = car.Parts[p].OffsetM.Z;
                    tempHeight += car.Parts[p].OffsetM.Y;
                }
                car.BogiePivotHeightM = tempHeight / (car.Parts.Count - 1);
            }

            car.SetUpWheels();

            // If we have two pantographs, 2 is the forwards pantograph, unlike when there's only one.
            if (!(car.Flipped ^ (car.Train != null && car.Train.IsActualPlayerTrain && Viewer.PlayerLocomotive.Flipped)) && !Pantograph1.Empty() && !Pantograph2.Empty())
                AnimatedPart.Swap(ref Pantograph1, ref Pantograph2);

            Pantograph1.SetState(MSTSWagon.Pantographs[1].CommandUp);
            Pantograph2.SetState(MSTSWagon.Pantographs[2].CommandUp);
            if (MSTSWagon.Pantographs.List.Count > 2) Pantograph3.SetState(MSTSWagon.Pantographs[3].CommandUp);
            if (MSTSWagon.Pantographs.List.Count > 3) Pantograph4.SetState(MSTSWagon.Pantographs[4].CommandUp);
            LeftDoor.SetState(MSTSWagon.LeftDoor.State >= DoorState.Opening);
            RightDoor.SetState(MSTSWagon.RightDoor.State >= DoorState.Opening);
            Mirrors.SetState(MSTSWagon.MirrorOpen);
            LeftWindowFront.SetState(MSTSWagon.WindowStates[MSTSWagon.LeftWindowFrontIndex] >= MSTSWagon.WindowState.Opening);
            RightWindowFront.SetState(MSTSWagon.WindowStates[MSTSWagon.RightWindowFrontIndex] >= MSTSWagon.WindowState.Opening);
            LeftWindowRear.SetState(MSTSWagon.WindowStates[MSTSWagon.LeftWindowRearIndex] >= MSTSWagon.WindowState.Opening);
            RightWindowRear.SetState(MSTSWagon.WindowStates[MSTSWagon.RightWindowRearIndex] >= MSTSWagon.WindowState.Opening);
            Item1TwoState.SetState(MSTSWagon.GenericItem1);
            Item2TwoState.SetState(MSTSWagon.GenericItem2);
            BrakeCylinders.SetFrameClamp(MSTSWagon.BrakeSystem.GetNormalizedCylTravel() * 10.0f);
            Handbrakes.SetState(MSTSWagon.GetTrainHandbrakeStatus());
            BrakeRigging.SetFrameClamp(Math.Max(MSTSWagon.BrakeSystem.GetNormalizedCylTravel(), MSTSWagon.GetTrainHandbrakeStatus() ? 1.0f : 0.0f) * 10.0f);
            UnloadingParts.SetState(MSTSWagon.UnloadingPartsOpen);

            InitializeUserInputCommands();
        }

        void MatchMatrixToPart(MSTSWagon car, int matrix, int bogieMatrix)
        {
            var matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].ToUpper();
            // Gate all RunningGearPartIndexes on this!
            var matrixAnimated = TrainCarShape.SharedShape.Animations != null && TrainCarShape.SharedShape.Animations.Count > 0 && TrainCarShape.SharedShape.Animations[0].anim_nodes.Count > matrix && TrainCarShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0;
            int? LinkedAxleIndex = null;
            int? notDrivenAxleIndex = null;
            int? drivenAxleIndex = null;
            for (int i=0; i<car.LocomotiveAxles.Count; i++)
            {
                if (car.LocomotiveAxles[i].AnimatedParts.Contains(matrixName))
                {
                    LinkedAxleIndex = i;
                    break;
                }
                // Do not attach parts to this axle by default if it contains a list of animated parts
                if (car.LocomotiveAxles[i].AnimatedParts.Count > 0) continue;
                if (notDrivenAxleIndex == null && car.LocomotiveAxles[i].DriveType == Simulation.RollingStocks.SubSystems.PowerTransmissions.AxleDriveType.NotDriven) notDrivenAxleIndex = i;
                if (drivenAxleIndex == null && car.LocomotiveAxles[i].DriveType != Simulation.RollingStocks.SubSystems.PowerTransmissions.AxleDriveType.NotDriven) drivenAxleIndex = i;
            }
            if (matrixName.StartsWith("WHEELS") && (matrixName.Length == 7 || matrixName.Length == 8 || matrixName.Length == 9))
            {
                // Standard WHEELS length would be 8 to test for WHEELS11. Came across WHEELS tag that used a period(.) between the last 2 numbers, changing max length to 9.
                // Changing max length to 9 is not a problem since the initial WHEELS test will still be good.
                var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                //someone uses wheel to animate fans, thus check if the wheel is not too high (lower than 3m), will animate it as real wheel
                if (m.M42 < 3)
                {
                    var id = 0;
                    // Model makers are not following the standard rules, For example, one tender uses naming convention of wheels11/12 instead of using Wheels1,2,3 when not part of a bogie.
                    // The next 2 lines will sort out these axles.
                    var tmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    if (matrixName.Length == 8 && bogieMatrix == 0 && tmatrix == 0) // In this test, both tmatrix and bogieMatrix are 0 since these wheels are not part of a bogie.
                        matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].Substring(0, 7); // Changing wheel name so that it reflects its actual use since it is not p
                    if (matrixName.Length == 8 || matrixName.Length == 9)
                        Int32.TryParse(matrixName.Substring(6, 1), out id);
                    if (matrixName.Length == 8 || matrixName.Length == 9 || !matrixAnimated)
                        WheelPartIndexes[LinkedAxleIndex ?? notDrivenAxleIndex ?? drivenAxleIndex ?? -1].Add(matrix);
                    else
                        RunningGears[LinkedAxleIndex ?? drivenAxleIndex ?? -1].AddMatrix(matrix);
                    var pmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    car.AddWheelSet(m.Translation, id, pmatrix, matrixName.ToString(), bogie1Axles, bogie2Axles);
                }
                // Standard wheels are processed above, but wheels used as animated fans that are greater than 3m are processed here.
                else
                    RunningGears[LinkedAxleIndex ?? -1].AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("BOGIE") && matrixName.Length <= 6) //BOGIE1 is valid, BOGIE11 is not, it is used by some modelers to indicate this is part of bogie1
            {
                if (matrixName.Length == 6)
                {
                    var id = 1;
                    Int32.TryParse(matrixName.Substring(5), out id);
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.Translation, matrix, id, matrixName.ToString(), numBogie1, numBogie2);
                    bogieMatrix = matrix; // Bogie matrix needs to be saved for test with axles.
                }
                else
                {
                    // Since the string content is BOGIE, Int32.TryParse(matrixName.Substring(5), out id) is not needed since its sole purpose is to
                    //  parse the string number from the string.
                    var id = 1;
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.Translation, matrix, id, matrixName.ToString(), numBogie1, numBogie2);
                    bogieMatrix = matrix; // Bogie matrix needs to be saved for test with axles.
                }
                // Bogies contain wheels!
                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i, bogieMatrix);
            }
            else if (matrixName.StartsWith("WIPER")) // wipers
            {
                Wipers.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("DOOR")) // doors (left / right)
            {
                if (matrixName.StartsWith("DOOR_D") || matrixName.StartsWith("DOOR_E") || matrixName.StartsWith("DOOR_F"))
                    LeftDoor.AddMatrix(matrix);
                else if (matrixName.StartsWith("DOOR_A") || matrixName.StartsWith("DOOR_B") || matrixName.StartsWith("DOOR_C"))
                    RightDoor.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTOGRAPH")) //pantographs (1/2)
            {

                switch (matrixName)
                {
                    case "PANTOGRAPHBOTTOM1":
                    case "PANTOGRAPHBOTTOM1A":
                    case "PANTOGRAPHBOTTOM1B":
                    case "PANTOGRAPHMIDDLE1":
                    case "PANTOGRAPHMIDDLE1A":
                    case "PANTOGRAPHMIDDLE1B":
                    case "PANTOGRAPHTOP1":
                    case "PANTOGRAPHTOP1A":
                    case "PANTOGRAPHTOP1B":
                        Pantograph1.AddMatrix(matrix);
                        break;
                    case "PANTOGRAPHBOTTOM2":
                    case "PANTOGRAPHBOTTOM2A":
                    case "PANTOGRAPHBOTTOM2B":
                    case "PANTOGRAPHMIDDLE2":
                    case "PANTOGRAPHMIDDLE2A":
                    case "PANTOGRAPHMIDDLE2B":
                    case "PANTOGRAPHTOP2":
                    case "PANTOGRAPHTOP2A":
                    case "PANTOGRAPHTOP2B":
                        Pantograph2.AddMatrix(matrix);
                        break;
                    default://someone used other language
                        if (matrixName.Contains("1"))
                            Pantograph1.AddMatrix(matrix);
                        else if (matrixName.Contains("2"))
                            Pantograph2.AddMatrix(matrix);
                        else if (matrixName.Contains("3"))
                            Pantograph3.AddMatrix(matrix);
                        else if (matrixName.Contains("4"))
                            Pantograph4.AddMatrix(matrix);
                        else
                        {
                            if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                            else Pantograph2.AddMatrix(matrix);
                        }
                        break;
                }
            }
            else if (matrixName.StartsWith("MIRROR")) // mirrors
            {
                Mirrors.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("LEFTWINDOWFRONT")) // Windows
            {
                LeftWindowFront.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("RIGHTWINDOWFRONT")) // Windows
            {
                RightWindowFront.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("LEFTWINDOWREAR")) // Windows
            {
                LeftWindowRear.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("RIGHTWINDOWREAR")) // Windows
            {
                RightWindowRear.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("UNLOADINGPARTS")) // unloading parts
            {
                UnloadingParts.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
            {
                Trace.TraceInformation("Pantograph matrix with unusual name {1} in shape {0}", TrainCarShape.SharedShape.FilePath, matrixName);
                if (matrixName.Contains("1"))
                    Pantograph1.AddMatrix(matrix);
                else if (matrixName.Contains("2"))
                    Pantograph2.AddMatrix(matrix);
                else if (matrixName.Contains("3"))
                    Pantograph3.AddMatrix(matrix);
                else if (matrixName.Contains("4"))
                    Pantograph4.AddMatrix(matrix);
                else
                {
                    if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                    else Pantograph2.AddMatrix(matrix);
                }
            }
            else if (matrixName.StartsWith("ORTSBELL")) // bell
            {
                Bell.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSITEM1CONTINUOUS")) // generic item 1, continuous animation
            {
                Item1Continuous.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSITEM2CONTINUOUS")) // generic item 2, continuous animation
            {
                Item2Continuous.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSITEM1TWOSTATE")) // generic item 1, continuous animation
            {
                Item1TwoState.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSITEM2TWOSTATE")) // generic item 2, continuous animation
            {
                Item2TwoState.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSBRAKECYLINDER")) // brake cylinder animation
            {
                BrakeCylinders.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSHANDBRAKE")) // handbrake wheel animation
            {
                Handbrakes.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("ORTSBRAKERIGGING")) // brake rigging/brake shoes animation
            {
                BrakeRigging.AddMatrix(matrix);
            }
            else
            {
                if (matrixAnimated && matrix != 0)
                    RunningGears[LinkedAxleIndex ?? drivenAxleIndex ?? -1].AddMatrix(matrix);

                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i, 0);
            }
        }

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommand.ControlPantograph1, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 1, !MSTSWagon.Pantographs[1].CommandUp) });
            UserInputCommands.Add(UserCommand.ControlPantograph2, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 2, !MSTSWagon.Pantographs[2].CommandUp) });
            if (MSTSWagon.Pantographs.List.Count > 2) UserInputCommands.Add(UserCommand.ControlPantograph3, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 3, !MSTSWagon.Pantographs[3].CommandUp) });
            if (MSTSWagon.Pantographs.List.Count > 3) UserInputCommands.Add(UserCommand.ControlPantograph4, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 4, !MSTSWagon.Pantographs[4].CommandUp) });
            UserInputCommands.Add(UserCommand.ControlDoorLeft, new Action[] { Noop, () => new ToggleDoorsLeftCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDoorRight, new Action[] { Noop, () => new ToggleDoorsRightCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlMirror, new Action[] { Noop, () => new ToggleMirrorsCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlWindowLeft, new Action[] { Noop, () => new ToggleWindowLeftCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlWindowRight, new Action[] { Noop, () => new ToggleWindowRightCommand(Viewer.Log) });
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            foreach (var command in UserInputCommands.Keys)
                if (UserInput.IsPressed(command)) UserInputCommands[command][1]();
                else if (UserInput.IsReleased(command)) UserInputCommands[command][0]();
        }

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Pantograph1.UpdateState(MSTSWagon.Pantographs[1].CommandUp, elapsedTime);
            Pantograph2.UpdateState(MSTSWagon.Pantographs[2].CommandUp, elapsedTime);
            if (MSTSWagon.Pantographs.List.Count > 2) Pantograph3.UpdateState(MSTSWagon.Pantographs[3].CommandUp, elapsedTime);
            if (MSTSWagon.Pantographs.List.Count > 3) Pantograph4.UpdateState(MSTSWagon.Pantographs[4].CommandUp, elapsedTime);
            LeftDoor.UpdateState(MSTSWagon.LeftDoor.State >= DoorState.Opening, elapsedTime);
            RightDoor.UpdateState(MSTSWagon.RightDoor.State >= DoorState.Opening, elapsedTime);
            Mirrors.UpdateState(MSTSWagon.MirrorOpen, elapsedTime);
            LeftWindowFront.UpdateState(MSTSWagon.WindowStates[MSTSWagon.LeftWindowFrontIndex] >= MSTSWagon.WindowState.Opening, elapsedTime);
            RightWindowFront.UpdateState(MSTSWagon.WindowStates[MSTSWagon.RightWindowFrontIndex] >= MSTSWagon.WindowState.Opening, elapsedTime);
            LeftWindowRear.UpdateState(MSTSWagon.WindowStates[MSTSWagon.LeftWindowRearIndex] >= MSTSWagon.WindowState.Opening, elapsedTime);
            RightWindowRear.UpdateState(MSTSWagon.WindowStates[MSTSWagon.RightWindowRearIndex] >= MSTSWagon.WindowState.Opening, elapsedTime);
            UnloadingParts.UpdateState(MSTSWagon.UnloadingPartsOpen, elapsedTime);
            Item1TwoState.UpdateState(MSTSWagon.GenericItem1, elapsedTime);
            Item2TwoState.UpdateState(MSTSWagon.GenericItem2, elapsedTime);
            BrakeCylinders.UpdateFrameClamp(MSTSWagon.BrakeSystem.GetNormalizedCylTravel() * 10.0f, elapsedTime, 10.0f);
            Handbrakes.UpdateState(MSTSWagon.GetTrainHandbrakeStatus(), elapsedTime);
            BrakeRigging.UpdateFrameClamp(Math.Max(MSTSWagon.BrakeSystem.GetNormalizedCylTravel(), MSTSWagon.GetTrainHandbrakeStatus() ? 1.0f : 0.0f) * 10.0f, elapsedTime, 10.0f);
            UpdateAnimation(frame, elapsedTime);

            var car = Car as MSTSWagon;
            // Steam leak in heating hose
            foreach (var drawer in HeatingHose)
            {
                drawer.SetOutput(car.HeatingHoseSteamVelocityMpS, car.HeatingHoseSteamVolumeM3pS, car.HeatingHoseParticleDurationS);
            }

            // Steam leak in heating compartment steamtrap
            foreach (var drawer in HeatingCompartmentSteamTrap)
            {
                drawer.SetOutput(car.HeatingCompartmentSteamTrapVelocityMpS, car.HeatingCompartmentSteamTrapVolumeM3pS, car.HeatingCompartmentSteamTrapParticleDurationS);
            }

            // Steam leak in heating main pipe steamtrap
            foreach (var drawer in HeatingMainPipeSteamTrap)
            {
                drawer.SetOutput(car.HeatingMainPipeSteamTrapVelocityMpS, car.HeatingMainPipeSteamTrapVolumeM3pS, car.HeatingMainPipeSteamTrapDurationS);
            }

            // Heating Steam Boiler Exhaust
            foreach (var drawer in HeatingSteamBoiler)
            {
                drawer.SetOutput(car.HeatingSteamBoilerVolumeM3pS, car.HeatingSteamBoilerDurationS, car.HeatingSteamBoilerSteadyColor);
            }

            // Exhaust for HEP/Electrical Generator
            foreach (var drawer in WagonGenerator)
            {
                drawer.SetOutput(car.WagonGeneratorVolumeM3pS, car.WagonGeneratorDurationS, car.WagonGeneratorSteadyColor);
            }

            // Wagon fire smoke
            foreach (var drawer in WagonSmoke)
            {
                drawer.SetOutput(car.WagonSmokeVelocityMpS, car.WagonSmokeVolumeM3pS, car.WagonSmokeDurationS, car.WagonSmokeSteadyColor);
            }

            if (car.Train != null) // only process this visual feature if this is a valid car in the train
            {
                // Water spray for water scoop (uses steam effects currently) - Forward direction
                if (car.Direction == Direction.Forward)
                {
                    foreach (var drawer in WaterScoop)
                    {
                        drawer.SetOutput(car.WaterScoopWaterVelocityMpS, car.WaterScoopWaterVolumeM3pS, car.WaterScoopParticleDurationS);
                    }
                }
                // If travelling in reverse turn on rearward facing effect
                else if (car.Direction == Direction.Reverse)
                {
                    foreach (var drawer in WaterScoopReverse)
                    {
                        drawer.SetOutput(car.WaterScoopWaterVelocityMpS, car.WaterScoopWaterVolumeM3pS, car.WaterScoopParticleDurationS);
                    }
                }
            }

            // Water overflow from tender (uses steam effects currently)
            foreach (var drawer in TenderWaterOverflow)
            {
                drawer.SetOutput(car.TenderWaterOverflowVelocityMpS, car.TenderWaterOverflowVolumeM3pS, car.TenderWaterOverflowParticleDurationS);
            }

            // Bearing Hot box smoke
            foreach (var drawer in BearingHotBox)
            {
                drawer.SetOutput(car.BearingHotBoxSmokeVelocityMpS, car.BearingHotBoxSmokeVolumeM3pS, car.BearingHotBoxSmokeDurationS, car.BearingHotBoxSmokeSteadyColor);
            }

            // Steam Brake effects
            foreach (var drawer in SteamBrake)
            {
                drawer.SetOutput(car.SteamBrakeLeaksVelocityMpS, car.SteamBrakeLeaksVolumeM3pS, car.SteamBrakeLeaksDurationS);
            }

            foreach (List<ParticleEmitterViewer> drawers in ParticleDrawers.Values)
                foreach (ParticleEmitterViewer drawer in drawers)
                    drawer.PrepareFrame(frame, elapsedTime);

            if (!(car is MSTSLocomotive) && (LeftWindowFront.MaxFrame > 0 || RightWindowFront.MaxFrame > 0))
            {
                if (LeftWindowFront.MaxFrame > 0)
                    car.SoundHeardInternallyCorrection[MSTSWagon.LeftWindowFrontIndex] = LeftWindowFront.AnimationKeyFraction();
                if (RightWindowFront.MaxFrame > 0)
                    car.SoundHeardInternallyCorrection[MSTSWagon.RightWindowFrontIndex] = RightWindowFront.AnimationKeyFraction();
            }
        }


        private void UpdateAnimation(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float distanceTravelledM; // Distance travelled by non-driven wheels
            float AnimationWheelRadiusM = MSTSWagon.WheelRadiusM; // Radius of non driven wheels
            float AnimationDriveWheelRadiusM = MSTSWagon.DriverWheelRadiusM; // Radius of driven wheels

            if (MSTSWagon is MSTSLocomotive loco)
            {
                //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                //                                distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
                distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && loco.UsingRearCab) ? -1 : 1) * MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
                foreach (var kvp in RunningGears)
                {
                    if (!kvp.Value.Empty())
                    {
                        var axle = kvp.Key >= 0 && kvp.Key < loco.LocomotiveAxles.Count ? loco.LocomotiveAxles[kvp.Key] : null;
                        if (axle != null)
                            //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                            kvp.Value.UpdateLoop(((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && loco.UsingRearCab) ? -1 : 1) * (float)axle.AxleSpeedMpS * elapsedTime.ClockSeconds / MathHelper.TwoPi / axle.WheelRadiusM);
                        else if (AnimationDriveWheelRadiusM > 0.001)
                            kvp.Value.UpdateLoop(distanceTravelledM / MathHelper.TwoPi / AnimationDriveWheelRadiusM);
                    }
                        
                }
                foreach (var kvp in WheelPartIndexes)
                {
                    var axle = kvp.Key < loco.LocomotiveAxles.Count && kvp.Key >= 0 ? loco.LocomotiveAxles[kvp.Key] : null;
                    if (axle != null)
                    {
                        //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                        WheelRotationR = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && loco.UsingRearCab) ? -1 : 1) * -(float)axle.AxlePositionRad;
                    }
                    else
                    {
                        var rotationalDistanceR = distanceTravelledM / AnimationWheelRadiusM;  // in radians
                        WheelRotationR = MathHelper.WrapAngle(WheelRotationR - rotationalDistanceR);
                    }
                    Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                    foreach (var iMatrix in kvp.Value)
                    {
                        TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
                    }
                }
            }
            else // set values for wagons (not handled by Axle class)
            {
                distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
                if (Car.BrakeSkid) distanceTravelledM = 0;
                foreach (var kvp in RunningGears)
                {
                    if (!kvp.Value.Empty() && AnimationDriveWheelRadiusM > 0.001)
                        kvp.Value.UpdateLoop(distanceTravelledM / MathHelper.TwoPi / AnimationDriveWheelRadiusM);
                }
                // Wheel rotation (animation) - for non-drive wheels in steam locomotives and all wheels in other stock
                if (WheelPartIndexes.Count > 0)
                {
                    var rotationalDistanceR = distanceTravelledM / AnimationWheelRadiusM;  // in radians
                    WheelRotationR = MathHelper.WrapAngle(WheelRotationR - rotationalDistanceR);
                    var wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                    foreach (var kvp in WheelPartIndexes)
                    {
                        foreach (var iMatrix in kvp.Value)
                        {
                            TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
                        }
                    }
                }
            }

#if DEBUG_WHEEL_ANIMATION

            Trace.TraceInformation("========================== Debug Animation in MSTSWagonViewer.cs ==========================================");
            Trace.TraceInformation("Slip speed - Car ID: {0} WheelDistance: {1}", Car.CarID, distanceTravelledM);
            Trace.TraceInformation("Wag Speed - Wheelspeed: {0} Slip: {1} Train: {2}", MSTSWagon.WheelSpeedMpS, MSTSWagon.WheelSpeedSlipMpS, MSTSWagon.SpeedMpS);
            Trace.TraceInformation("Wheel Radius - DriveWheel: {0} NonDriveWheel: {1}", AnimationDriveWheelRadiusM, AnimationWheelRadiusM);

#endif

            // Bogie angle animation
            Matrix inverseLocation = Matrix.Invert(Car.WorldPosition.XNAMatrix);

            foreach (var p in Car.Parts)
            {
                if (p.iMatrix <= 0 || p.iMatrix >= TrainCarShape.SharedShape.Matrices.Count())
                    continue;

                Matrix m = Matrix.Identity;

                // Bogie rotation calculation doesn't work on turntables
                // Assume bogies aren't rotated when on a turntable
                if (Car.Train?.ControlMode != Train.TRAIN_CONTROL.TURNTABLE)
                {
                    // Determine orientation of bogie in absolute space
                    Vector3 fwd = new Vector3(p.Dir[0], p.Dir[1], -p.Dir[2]);
                    // Only do this calculation if the bogie position has been calculated
                    if (!(fwd.X == 0 && fwd.Y == 0 && fwd.Z == 0))
                    {
                        fwd.Normalize();
                        Vector3 side = Vector3.Cross(Vector3.Up, fwd);
                        if (!(side.X == 0 && side.Y == 0 && side.Z == 0))
                            side.Normalize();
                        Vector3 up = Vector3.Cross(fwd, side);
                        m.Right = side;
                        m.Up = up;
                        m.Backward = fwd;

                        // Convert absolute rotation into rotation relative to train car
                        m = Matrix.CreateRotationZ(p.Roll) * m * inverseLocation;
                    }
                }
                // Insert correct translation (previous step likely introduced garbage data)
                m.Translation = TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;

                // To cancel out any vibration, apply the inverse here. If no vibration is present, this matrix will be Matrix.Identity.
                TrainCarShape.XNAMatrices[p.iMatrix] = Car.VibrationInverseMatrix * m;
            }

            if ((MSTSWagon.Train?.IsPlayerDriven ?? false) && !Car.Simulator.Settings.SimpleControlPhysics)
            {
                UpdateCouplers(frame, elapsedTime);
            }

            // Applies MSTS style freight animation for coal load on the locomotive, crews, and other static animations.
            // Takes the form of FreightAnim ( A B C )
            // MSTS allowed crew figures to be inserted into the tender WAG file and thus be displayed on the locomotive.
            // It appears that only one MSTS type FA can be used per vehicle (to be confirmed?)
            // For coal load variation, C should be absent (set to 1 when read in WAG file) or >0 - sets FreightAnimFlag; and A > B
            // To disable coal load variation and insert a static (crew) shape on the tender breech, one of the conditions indicated above
            if (FreightShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // Define default position of shape
                FreightShape.Location.XNAMatrix = Car.WorldPosition.XNAMatrix;
                FreightShape.Location.TileX = Car.WorldPosition.TileX;
                FreightShape.Location.TileZ = Car.WorldPosition.TileZ;

                    bool SteamAnimShape = false;
                    float FuelControllerLevel = 0.0f;

                // For coal load variation on locomotives determine the current fuel level - and whether locomotive is a tender or tank type locomotive.
                if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSSteamLocomotive)
                {

                    var NonTenderSteamLocomotive = MSTSWagon as MSTSSteamLocomotive;

                    if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSLocomotive && (MSTSWagon.EngineType == TrainCar.EngineTypes.Steam && NonTenderSteamLocomotive.IsTenderRequired == 0.0))
                    {

                        if (MSTSWagon.TendersSteamLocomotive == null)
                            MSTSWagon.FindTendersSteamLocomotive();

                        if (MSTSWagon.TendersSteamLocomotive != null)
                        {
                            FuelControllerLevel = MSTSWagon.TendersSteamLocomotive.FuelController.CurrentValue;
                            SteamAnimShape = true;
                        }
                        else if (NonTenderSteamLocomotive != null)
                        {
                            FuelControllerLevel = NonTenderSteamLocomotive.FuelController.CurrentValue;
                            SteamAnimShape = true;
                        } 
                    }
                }

                    // Set height of FAs - if relevant conditions met, use default position co-ords defined above
                    if (FreightShape.XNAMatrices.Length > 0)
                    {
                        // For tender coal load animation 
                        if (MSTSWagon.FreightAnimFlag > 0 && MSTSWagon.FreightAnimMaxLevelM > MSTSWagon.FreightAnimMinLevelM && SteamAnimShape)
                        {
                            FreightShape.XNAMatrices[0].M42 = MSTSWagon.FreightAnimMinLevelM + FuelControllerLevel * (MSTSWagon.FreightAnimMaxLevelM - MSTSWagon.FreightAnimMinLevelM);
                        }
                        // reproducing MSTS strange behavior; used to display loco crew when attached to tender
                        else if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender) 
                        {
                            FreightShape.Location.XNAMatrix.M42 += MSTSWagon.FreightAnimMaxLevelM;
                        }
                    }
                // Display Animation Shape                    
                FreightShape.PrepareFrame(frame, elapsedTime);
            }

            if (FreightAnimations != null)
            {
                foreach (var freightAnim in FreightAnimations.Animations)
                {
                    if (freightAnim.Animation is FreightAnimationStatic)
                    {
                        var animation = freightAnim.Animation as FreightAnimationStatic;
                        if (!((animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Cab3D] &&
                            Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab) ||
                            (animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Cab2D] &&
                            Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.Cab) ||
                            (animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Outside] && (Viewer.Camera.AttachedCar != this.MSTSWagon ||
                            (Viewer.Camera.Style != Camera.Styles.ThreeDimCab && Viewer.Camera.Style != Camera.Styles.Cab))))) continue;
                    }
                    if (freightAnim.FreightShape != null && !((freightAnim.Animation is FreightAnimationContinuous) && (freightAnim.Animation as FreightAnimationContinuous).LoadPerCent == 0))
                    {
                        freightAnim.FreightShape.Location.XNAMatrix = Car.WorldPosition.XNAMatrix;
                        freightAnim.FreightShape.Location.TileX = Car.WorldPosition.TileX; freightAnim.FreightShape.Location.TileZ = Car.WorldPosition.TileZ;
                        if (freightAnim.FreightShape.XNAMatrices.Length > 0)
                        {
                            if (freightAnim.Animation is FreightAnimationContinuous)
                            {
                                var continuousFreightAnim = freightAnim.Animation as FreightAnimationContinuous;
                                if (MSTSWagon.FreightAnimations.IsGondola) freightAnim.FreightShape.XNAMatrices[0] = TrainCarShape.XNAMatrices[1];
                                freightAnim.FreightShape.XNAMatrices[0].M42 = continuousFreightAnim.MinHeight +
                                   continuousFreightAnim.LoadPerCent / 100 * (continuousFreightAnim.MaxHeight - continuousFreightAnim.MinHeight);
                            }
                            if (freightAnim.Animation is FreightAnimationStatic)
                            {
                                var staticFreightAnim = freightAnim.Animation as FreightAnimationStatic;
                                freightAnim.FreightShape.XNAMatrices[0].M41 = staticFreightAnim.XOffset;
                                freightAnim.FreightShape.XNAMatrices[0].M42 = staticFreightAnim.YOffset;
                                freightAnim.FreightShape.XNAMatrices[0].M43 = staticFreightAnim.ZOffset;
                            }

                        }
                        // Forcing rotation of freight shape
                        freightAnim.FreightShape.PrepareFrame(frame, elapsedTime);
                    }
                }
            }

            // Get the current height above "sea level" for the relevant car
            Car.CarHeightAboveSeaLevelM = Viewer.Tiles.GetElevation(Car.WorldPosition.WorldLocation);

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.Style == Camera.Styles.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime);
            }
            else
            {
                // Skip drawing if 2D or 3D Cab view - Cab view already drawn - by GeorgeS changed by DennisAT
                if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                    (Viewer.Camera.Style == Camera.Styles.Cab || Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
                    return;

                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime);
            }

        }

        /// <summary>
        /// Position couplers at each end of car and adjust their angle to mate with adjacent car
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="elapsedTime"></param>
        private void UpdateCouplers(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Display front coupler in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (FrontCouplerShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                var displacement =  Car.FrontCoupler.Size;
                displacement.Z += (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM - Car.WagonFrontCouplerCurveExtM;

                if (Car.CarAhead != null) // Display animated coupler if there is a car infront of this car
                {
                    var quaternion = PositionCoupler(Car, FrontCouplerShape, displacement);

                    var quaternionCar = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

                    var CouplerAngleRadians = Car.AdjustedWagonFrontCouplerAngleRad;

                    AlignCouplerWithCar(Car, FrontCouplerShape);

                    AdjustCouplerAngle(Car, FrontCouplerShape, quaternionCar, CouplerAngleRadians);

                    // If the car ahead does not have an animated coupler then location values will be zero for car ahaead, and no coupler will display. Hence do not correct coupler location 
                    if (Car.CarAhead.RearCouplerLocation.X != 0 && Car.CarAhead.RearCouplerLocation.Y != 0 && Car.CarAhead.RearCouplerLocation.Z != 0)
                    {
                        // Next section tests front coupler against rear coupler on previous car. If they are not located at the same position, then location is set the same as previous car.
                        // For some reason flipped cars have a small location error, and hence couplers do not align.
                        var absXc = Math.Abs(FrontCouplerShape.Location.Location.X - Car.CarAhead.RearCouplerLocation.X);
                        var absYc = Math.Abs(FrontCouplerShape.Location.Location.Y - Car.CarAhead.RearCouplerLocation.Y);
                        var absZc = Math.Abs(FrontCouplerShape.Location.Location.Z - Car.CarAhead.RearCouplerLocation.Z);

                        if ((absXc > 0.005 || absYc > 0.005 || absZc > 0.005))
                        {
                            FrontCouplerShape.Location.Location = Car.CarAhead.RearCouplerLocation; // Set coupler to same location as previous car coupler
                            FrontCouplerShape.Location.TileX = Car.CarAhead.RearCouplerLocationTileX;
                            FrontCouplerShape.Location.TileZ = Car.CarAhead.RearCouplerLocationTileZ;
                        }
                    }

                    // Display Animation Shape                    
                    FrontCouplerShape.PrepareFrame(frame, elapsedTime);
                }
                else if (FrontCouplerOpenShape != null && Car.FrontCoupler.IsOpen) // Display open coupler if no car in front of car, and an open coupler shape is present
                {
                    var quaternion = PositionCoupler(Car, FrontCouplerOpenShape, displacement);

                    AlignCouplerWithCar(Car, FrontCouplerOpenShape);

                    // Display Animation Shape                    
                    FrontCouplerOpenShape.PrepareFrame(frame, elapsedTime);
                }
                else //Display closed static coupler by default if other conditions not met
                {
                    var quaternion = PositionCoupler(Car, FrontCouplerShape, displacement);

                    AlignCouplerWithCar(Car, FrontCouplerShape);

                    // Display Animation Shape                    
                    FrontCouplerShape.PrepareFrame(frame, elapsedTime);
                }

            }

            // Display rear coupler in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (RearCouplerShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                var displacement = Car.RearCoupler.Size;
                displacement.Z += (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM - Car.WagonRearCouplerCurveExtM;
                displacement.Z *= -1; // Reversed as this is the rear coupler of the wagon

                if (Car.CarBehind != null) // Display animated coupler if there is a car behind this car
                {
                    var quaternion = PositionCoupler(Car, RearCouplerShape, displacement);

                    var quaternionCar = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

                    var CouplerAngleRadians = Car.AdjustedWagonRearCouplerAngleRad;

                    AlignCouplerWithCar(Car, RearCouplerShape);

                    AdjustCouplerAngle(Car, RearCouplerShape, quaternionCar, CouplerAngleRadians);

                    // Display Animation Shape                    
                    RearCouplerShape.PrepareFrame(frame, elapsedTime);

                    // Save coupler location for use on following car front coupler
                    Car.RearCouplerLocation = RearCouplerShape.Location.Location;
                    Car.RearCouplerLocationTileX = RearCouplerShape.Location.TileX;
                    Car.RearCouplerLocationTileZ = RearCouplerShape.Location.TileZ;

                }
                else if (RearCouplerOpenShape != null && Car.RearCoupler.IsOpen) // Display open coupler if no car is behind car, and an open coupler shape is present
                {
                    var quaternion = PositionCoupler(Car, RearCouplerOpenShape, displacement);

                    AlignCouplerWithCar(Car, RearCouplerOpenShape);

                    // Display Animation Shape                    
                    RearCouplerOpenShape.PrepareFrame(frame, elapsedTime);
                }
                else //Display closed static coupler by default if other conditions not met
                {
                    var quaternion = PositionCoupler(Car, RearCouplerShape, displacement);

                    AlignCouplerWithCar(Car, RearCouplerShape);

                    // Display Animation Shape                    
                    RearCouplerShape.PrepareFrame(frame, elapsedTime);
                }
            }

            // Display front airhose in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (FrontAirHoseShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {

                if (Car.CarAhead != null) // Display animated coupler if there is a car behind this car
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.FrontAirHose.Size.X,
                        Y = Car.FrontAirHose.Size.Y + Car.FrontAirHose.HeightAdjustmentM,
                        Z = (Car.FrontCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM)
                    };

                    var quaternion = PositionCoupler(Car, FrontAirHoseShape, displacement);

                    var quaternionCar = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

                    var AirHoseYAngleRadians = Car.FrontAirHose.YAngleAdjustmentRad;
                    var AirHoseZAngleRadians = Car.FrontAirHose.ZAngleAdjustmentRad;

                    AlignCouplerWithCar(Car, FrontAirHoseShape);

                    AdjustAirHoseAngle(Car, FrontAirHoseShape, quaternionCar, AirHoseYAngleRadians, AirHoseZAngleRadians);

                    // Display Animation Shape                    
                    FrontAirHoseShape.PrepareFrame(frame, elapsedTime);

                }
                else if (FrontAirHoseDisconnectedShape != null && Car.RearCoupler.IsOpen) // Display open coupler if no car is behind car, and an open coupler shape is present
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.FrontAirHose.Size.X,
                        Y = Car.FrontAirHose.Size.Y,
                        Z = (Car.FrontCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM)
                    };

                    var quaternion = PositionCoupler(Car, FrontAirHoseDisconnectedShape, displacement);

                    AlignCouplerWithCar(Car, FrontAirHoseDisconnectedShape);

                    // Display Animation Shape                    
                    FrontAirHoseDisconnectedShape.PrepareFrame(frame, elapsedTime);
                }
                else //Display closed static coupler by default if other conditions not met
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.FrontAirHose.Size.X,
                        Y = Car.FrontAirHose.Size.Y,
                        Z = (Car.FrontCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM)
                    };
                    var quaternion = PositionCoupler(Car, FrontAirHoseShape, displacement);

                    AlignCouplerWithCar(Car, FrontAirHoseShape);

                    // Display Animation Shape                    
                    FrontAirHoseShape.PrepareFrame(frame, elapsedTime);
                }
            }


            // Display rear airhose in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (RearAirHoseShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {

                if (Car.CarBehind != null) // Display animated air hose if there is a car behind this car
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.RearAirHose.Size.X,
                        Y = Car.RearAirHose.Size.Y + Car.RearAirHose.HeightAdjustmentM,
                        Z = -(Car.RearCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM)  // Reversed as this is the rear coupler of the wagon
                    };

                    var quaternion = PositionCoupler(Car, RearAirHoseShape, displacement);

                    var quaternionCar = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

                    var AirHoseYAngleRadians = Car.RearAirHose.YAngleAdjustmentRad;
                    var AirHoseZAngleRadians = -Car.RearAirHose.ZAngleAdjustmentRad;

                    AlignCouplerWithCar(Car, RearAirHoseShape);

                    AdjustAirHoseAngle(Car, RearAirHoseShape, quaternionCar, AirHoseYAngleRadians, AirHoseZAngleRadians);

                    // Display Animation Shape                    
                    RearAirHoseShape.PrepareFrame(frame, elapsedTime);

                }
                else if (RearAirHoseDisconnectedShape != null && Car.RearCoupler.IsOpen) // Display single air hose if no car is behind car, and an open air hose shape is present
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.RearAirHose.Size.X,
                        Y = Car.RearAirHose.Size.Y,
                        Z = -(Car.RearCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM)  // Reversed as this is the rear coupler of the wagon
                    };

                    var quaternion = PositionCoupler(Car, RearAirHoseDisconnectedShape, displacement);

                    AlignCouplerWithCar(Car, RearAirHoseDisconnectedShape);

                    // Display Animation Shape                    
                    RearAirHoseDisconnectedShape.PrepareFrame(frame, elapsedTime);
                }
                else //Display closed static air hose by default if other conditions not met
                {
                    // Get the movement that would be needed to locate the air hose on the car if they were pointing in the default direction.
                    var displacement = new Vector3
                    {
                        X = Car.RearAirHose.Size.X,
                        Y = Car.RearAirHose.Size.Y,
                        Z = -(Car.RearCoupler.Size.Z + (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM)  // Reversed as this is the rear coupler of the wagon
                    };

                    var quaternion = PositionCoupler(Car, RearAirHoseShape, displacement);

                    AlignCouplerWithCar(Car, RearAirHoseShape);

                    // Display Animation Shape                    
                    RearAirHoseShape.PrepareFrame(frame, elapsedTime);
                }
            }


        }

        /// <summary>
        /// Positions the coupler at the at the centre of the car (world position), and then rotates it to the end of the car.
        /// Returns a quaternion for the car.
        /// </summary>
        /// <param name="car"></param>
        /// <param name="couplerShape"></param>
        /// <param name="displacement"></param>
        /// <returns></returns>
        private Quaternion PositionCoupler(TrainCar car, AnimatedShape couplerShape, Vector3 displacement)
        {
            // ToDO - For some reason aligning the coupler with a flipped car introduces a small error in the coupler position such that the couplers between a normal and flipped 
            // car will not align correctly.
            // To correct this "somewhat" a test has been introduced to align coupler location with the previous car. See code above in front coupler.
            
            // Place the coupler in the centre of the car
            var p = new WorldPosition(car.WorldPosition);
            couplerShape.Location.Location = new Vector3(p.Location.X, p.Location.Y, p.Location.Z);
            couplerShape.Location.TileX = p.TileX;
            couplerShape.Location.TileZ = p.TileZ;

            if (car.Flipped)
            {
                p.XNAMatrix.M11 *= -1;
                p.XNAMatrix.M13 *= -1;
                p.XNAMatrix.M21 *= -1;
                p.XNAMatrix.M23 *= -1;
                p.XNAMatrix.M31 *= -1;
                p.XNAMatrix.M33 *= -1;
            }

            // Get the orientation of the car as a quaternion
            p.XNAMatrix.Decompose(out Vector3 scale, out Quaternion quaternion, out Vector3 translation);

            // Reverse the y axis (plan view) component - perhaps because XNA is opposite to MSTS
            var quaternionReversed = new Quaternion(-quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);

            Vector3 rotatedDisplacement;

            // Rotate the displacement to match the orientation of the car
            rotatedDisplacement = Vector3.Transform(displacement, quaternionReversed);

            // Apply the rotation to the coupler's displacement to swing it round to the end of the wagon
            couplerShape.Location.Location += rotatedDisplacement;

            return quaternion;
        }

        /// <summary>
        /// Turn coupler the required angle between the cars
        /// </summary>
        /// <param name="adjacentCar"></param>
        /// <param name="couplerShape"></param>
        /// <param name="quaternionCar"></param>
        private void AdjustCouplerAngle(TrainCar adjacentCar, AnimatedShape couplerShape, Quaternion quaternionCar, float angle)
        {
            var mRotation = Matrix.CreateRotationY(angle);

            // Rotate the coupler to align with the calculated angle direction
            couplerShape.Location.XNAMatrix = mRotation* couplerShape.Location.XNAMatrix;

        }

        /// <summary>
        /// Turn coupler the required angle between the cars
        /// </summary>
        /// <param name="adjacentCar"></param>
        /// <param name="couplerShape"></param>
        /// <param name="quaternionCar"></param>
        private void AdjustAirHoseAngle(TrainCar adjacentCar, AnimatedShape airhoseShape, Quaternion quaternionCar, float angley, float anglez)
        {
            var zRotation = Matrix.CreateRotationZ(anglez);

            // Rotate the airhose to align with the calculated angle direction
            airhoseShape.Location.XNAMatrix = zRotation * airhoseShape.Location.XNAMatrix;

            var yRotation = Matrix.CreateRotationY(angley);

            // Rotate the airhose to align with the calculated angle direction
            airhoseShape.Location.XNAMatrix = yRotation * airhoseShape.Location.XNAMatrix;

        }

    /// <summary>
    /// Rotate the coupler to align with the direction (attitude) of the car.
    /// </summary>
    /// <param name="car"></param>
    /// <param name="couplerShape"></param>
    private void AlignCouplerWithCar(TrainCar car, AnimatedShape couplerShape)
        {

            var p = new WorldPosition(car.WorldPosition);

            if (car.Flipped)
            {
                p.XNAMatrix.M11 *= -1;
                p.XNAMatrix.M13 *= -1;
                p.XNAMatrix.M21 *= -1;
                p.XNAMatrix.M23 *= -1;
                p.XNAMatrix.M31 *= -1;
                p.XNAMatrix.M33 *= -1;
            }

            // Align the coupler shape
            couplerShape.Location.XNAMatrix.M11 = p.XNAMatrix.M11;
            couplerShape.Location.XNAMatrix.M12 = p.XNAMatrix.M12;
            couplerShape.Location.XNAMatrix.M13 = p.XNAMatrix.M13;
            couplerShape.Location.XNAMatrix.M21 = p.XNAMatrix.M21;
            couplerShape.Location.XNAMatrix.M22 = p.XNAMatrix.M22;
            couplerShape.Location.XNAMatrix.M23 = p.XNAMatrix.M23;
            couplerShape.Location.XNAMatrix.M31 = p.XNAMatrix.M31;
            couplerShape.Location.XNAMatrix.M32 = p.XNAMatrix.M32;
            couplerShape.Location.XNAMatrix.M33 = p.XNAMatrix.M33;

        }

        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public override void Unload()
        {
            // Removing sound sources from sound update thread
            Viewer.SoundProcess.RemoveSoundSources(this);

            base.Unload();
        }


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        private void LoadCarSounds(string wagonFolderSlash)
        {
            if (MSTSWagon.MainSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.MainSoundFileName);
            if (MSTSWagon.InteriorSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
            if (MSTSWagon.Cab3DSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
        }


        /// <summary>
        /// Load the car sound, attach it to the car
        /// check first in the wagon folder, then the global folder for the sound.
        /// If not found, report a warning.
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        /// <param name="filename"></param>
        protected void LoadCarSound(string wagonFolderSlash, string filename)
        {
            if (filename == null)
                return;
            string smsFilePath = wagonFolderSlash + @"sound\" + filename;
            if (!File.Exists(smsFilePath))
                smsFilePath = Viewer.Simulator.BasePath + @"\sound\" + filename;
            if (!File.Exists(smsFilePath))
            {
                Trace.TraceWarning("Cannot find {1} car sound file {0}", filename, wagonFolderSlash);
                return;
            }

            try
            {
                Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, smsFilePath));
                if (MSTSWagon is MSTSLocomotive && MSTSWagon.Train != null && MSTSWagon.Train.TrainType == Train.TRAINTYPE.AI)
                {
                    if (MSTSWagon.CarID == MSTSWagon.Train.Cars[0].CarID)
                    // Lead loco, enable AI train trigger
                        MSTSWagon.SignalEvent(Event.AITrainLeadLoco);
                    // AI train helper loco
                    else MSTSWagon.SignalEvent(Event.AITrainHelperLoco);
                }
                else if (MSTSWagon == Viewer.PlayerLocomotive)
                    MSTSWagon.SignalEvent(Event.PlayerTrainLeadLoco);
                else if (MSTSWagon is MSTSLocomotive && MSTSWagon.Train != null && (MSTSWagon.Train.TrainType == Train.TRAINTYPE.PLAYER ||
                    MSTSWagon.Train.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN || MSTSWagon.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING))
                    MSTSWagon.SignalEvent(Event.PlayerTrainHelperLoco);
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(smsFilePath, error));
            }
        }

        /// <summary>
        /// Load the inside and outside sounds for the default level 0 track type.
        /// </summary>
        private void LoadTrackSounds()
        {
            if (Viewer.TrackTypes.Count > 0)  // TODO, still have to figure out if this should be part of the car, or train, or track
            {
                if (!string.IsNullOrEmpty(MSTSWagon.InteriorSoundFileName))
                    LoadTrackSound(Viewer.TrackTypes[0].InsideSound);

                LoadTrackSound(Viewer.TrackTypes[0].OutsideSound);
            }
        }

        /// <summary>
        /// Load the sound source, attach it to the car.
        /// Check first in route\SOUND folder, then in base\SOUND folder.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadTrackSound(string filename)
        {
            if (filename == null)
                return;
            string path = Viewer.Simulator.RoutePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
                path = Viewer.Simulator.BasePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
            {
                Trace.TraceWarning("Cannot find track sound file {0}", filename);
                return;
            }
            Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, path));
        }

        /// <summary>
        /// Checks this wagon viewer for stale shapes and sets the stale data flag if any shapes are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public override bool CheckStaleShapes()
        {
            if (!Car.StaleViewer)
            {
                // Wagons can use a variety of shapes, need to check if any of these are out of date
                if ((TrainCarShape != null && TrainCarShape.SharedShape.StaleData) ||
                    (FreightShape != null && FreightShape.SharedShape.StaleData) ||
                    (InteriorShape != null && InteriorShape.SharedShape.StaleData) ||
                    (FrontCouplerShape != null && FrontCouplerShape.SharedShape.StaleData) ||
                    (FrontCouplerOpenShape != null && FrontCouplerOpenShape.SharedShape.StaleData) ||
                    (FrontAirHoseShape != null && FrontAirHoseShape.SharedShape.StaleData) ||
                    (FrontAirHoseDisconnectedShape != null && FrontAirHoseDisconnectedShape.SharedShape.StaleData) ||
                    (RearCouplerShape != null && RearCouplerShape.SharedShape.StaleData) ||
                    (RearCouplerOpenShape != null && RearCouplerOpenShape.SharedShape.StaleData) ||
                    (RearAirHoseShape != null && RearAirHoseShape.SharedShape.StaleData) ||
                    (RearAirHoseDisconnectedShape != null && RearAirHoseDisconnectedShape.SharedShape.StaleData))
                {
                    Car.StaleViewer = true;
                }
                else if (FreightAnimations != null)
                {
                    foreach (FreightAnimationViewer animation in FreightAnimations.Animations)
                    {
                        if (animation.FreightShape != null && animation.FreightShape.SharedShape.StaleData)
                        {
                            Car.StaleViewer = true;

                            break;
                        }
                    }
                }

                return Car.StaleViewer;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks this wagon viewer for stale directly-referenced textures and sets the stale data flag if any textures are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public override bool CheckStaleTextures()
        {
            if (!Car.StaleViewer)
            {
                // Textures referenced directly by the viewer include light textures and particle textures
                // as opposed to textures referenced indirectly through shape files
                if (!base.CheckStaleTextures())
                {
                    foreach (List<ParticleEmitterViewer> emitters in ParticleDrawers.Values)
                    {
                        foreach (ParticleEmitterViewer emitter in emitters)
                        {
                            if (emitter.CheckStale())
                            {
                                Car.StaleViewer = true;
                                break;
                            }
                        }
                        if (Car.StaleViewer)
                            break;
                    }
                }

                return Car.StaleViewer;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks this wagon viewer for stale sounds and sets the stale data flag if any sounds are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public override bool CheckStaleSounds()
        {
            if (!Car.StaleViewer)
            {
                Car.StaleViewer = Viewer.SoundProcess.GetStale(this);

                return Car.StaleViewer;
            }
            else
                return false;
        }

        internal override void Mark()
        {
            TrainCarShape.Mark();
            FreightShape?.Mark();
            InteriorShape?.Mark();
            FreightAnimations?.Mark();
            FrontCouplerShape?.Mark();
            FrontCouplerOpenShape?.Mark();
            RearCouplerShape?.Mark();
            RearCouplerOpenShape?.Mark();
            FrontAirHoseShape?.Mark();
            FrontAirHoseDisconnectedShape?.Mark();
            RearAirHoseShape?.Mark();
            RearAirHoseDisconnectedShape?.Mark();

            foreach (var pdl in ParticleDrawers.Values)
            {
                foreach (var pd in pdl)
                {
                    pd.Mark();
                }
            }
        }
    }
}
