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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.Viewer3D.RollingStock
{
    public class MSTSWagonViewer : TrainCarViewer
    {
        protected PoseableShape TrainCarShape;
        protected AnimatedShape FreightShape;
        protected AnimatedShape InteriorShape;
        public static readonly Action Noop = () => { };
        /// <summary>
        /// Dictionary of built-in locomotive control keyboard commands, Action[] is in the order {KeyRelease, KeyPress}
        /// </summary>
        public Dictionary<UserCommands, Action[]> UserInputCommands = new Dictionary<UserCommands, Action[]>();

        // Wheels are rotated by hand instead of in the shape file.
        float WheelRotationR;
        List<int> WheelPartIndexes = new List<int>();

        // Everything else is animated through the shape file.
        AnimatedPart RunningGear;
        AnimatedPart Pantograph1;
        AnimatedPart Pantograph2;
        AnimatedPart LeftDoor;
        AnimatedPart RightDoor;
        AnimatedPart Mirrors;
        protected AnimatedPart Wipers;

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon)Car; } }

        bool HasFirstPanto;
        int numWheels1, numWheels2, numBogie1, numBogie2, numBogie = 0;
        public MSTSWagonViewer(Viewer viewer, MSTSWagon car)
            : base(viewer, car)
        {
            var wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";

            TrainCarShape = car.MainShapeFileName != string.Empty
                ? new PoseableShape(viewer, wagonFolderSlash + car.MainShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster)
                : new PoseableShape(viewer, null, car.WorldPosition);

            if (car.FreightShapeFileName != null)
            {
                car.HasFreightAnim = true;
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
                // Reproducing MSTS "bug" of not allowing tender animation in case both minLevel and maxLevel is 0
                if (MSTSWagon.IsTender && MSTSWagon.FreightAnimMaxLevelM != 0)
                {
                    // Force allowing animation:
                    if (FreightShape.SharedShape.LodControls.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy.Length > 0)
                        FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] = 1;
                }
            }
            if (car.InteriorShapeFileName != null)
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.Interior);

            RunningGear = new AnimatedPart(TrainCarShape);
            Pantograph1 = new AnimatedPart(TrainCarShape);
            Pantograph2 = new AnimatedPart(TrainCarShape);
            LeftDoor = new AnimatedPart(TrainCarShape);
            RightDoor = new AnimatedPart(TrainCarShape);
            Mirrors = new AnimatedPart(TrainCarShape);
            Wipers = new AnimatedPart(TrainCarShape);

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
                    numBogie1 += 1;
                else if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE2"))
                    numBogie2 += 1;
                else if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE"))
                    numBogie += 1;
                else if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS"))
                    if (TrainCarShape.SharedShape.MatrixNames[i].Length == 8)
                    {
                        if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS1"))
                            numWheels1 += 1;
                        else if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS2"))
                            numWheels2 += 1;
                    }
            }

            // Match up all the matrices with their parts.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.Hierarchy[i] == -1)
                    MatchMatrixToPart(car, i);

            car.SetUpWheels();

            // If we have two pantographs, 2 is the forwards pantograph, unlike when there's only one.
            if (!car.Flipped && !Pantograph1.Empty() && !Pantograph2.Empty())
                AnimatedPart.Swap(ref Pantograph1, ref Pantograph2);

            // If the car is flipped, the doors should be corrected to match the rest of the train.
            if (car.Flipped)
                AnimatedPart.Swap(ref RightDoor, ref LeftDoor);

            Pantograph1.SetState(MSTSWagon.Pantographs[1].CommandUp);
            Pantograph2.SetState(MSTSWagon.Pantographs[2].CommandUp);
            LeftDoor.SetState(MSTSWagon.DoorLeftOpen);
            RightDoor.SetState(MSTSWagon.DoorRightOpen);
            Mirrors.SetState(MSTSWagon.MirrorOpen);

            InitializeUserInputCommands();
        }

        void MatchMatrixToPart(MSTSWagon car, int matrix)
        {
            var matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].ToUpper();
            // Gate all RunningGearPartIndexes on this!
            var matrixAnimated = TrainCarShape.SharedShape.Animations != null && TrainCarShape.SharedShape.Animations.Count > 0 && TrainCarShape.SharedShape.Animations[0].anim_nodes.Count > matrix && TrainCarShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0;
            if (matrixName.StartsWith("WHEELS") && (matrixName.Length == 7 || matrixName.Length == 8 || matrixName.Length == 9))
            {
                // Standard WHEELS length would be 8 to test for WHEELS11. Came across WHEELS tag that used a period(.) between the last 2 numbers, changing max length to 9.
                // Changing max length to 9 is not a problem since the initial WHEELS test will still be good.
                var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                //someone uses wheel to animate fans, thus check if the wheel is not too high (lower than 3m), will animate it as real wheel
                if (m.M42 < 3)
                {
                    var id = 0;
                    if (matrixName.Length == 8 || matrixName.Length == 9)
                        Int32.TryParse(matrixName.Substring(6, 1), out id);
                    if (matrixName.Length == 8 || matrixName.Length == 9 || !matrixAnimated)
                        WheelPartIndexes.Add(matrix);
                    else
                        RunningGear.AddMatrix(matrix);
                    var pmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    car.AddWheelSet(m.M43, id, pmatrix, matrixName.ToString(), numWheels1, numWheels2);
                }
            }
            else if (matrixName.StartsWith("BOGIE") && matrixName.Length <= 6) //BOGIE1 is valid, BOGIE11 is not, it is used by some modelers to indicate this is part of bogie1
            {
                if (matrixName.Length == 6)
                {
                    var id = 1;
                    Int32.TryParse(matrixName.Substring(5), out id);
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43, matrix, id, matrixName.ToString(), numBogie1, numBogie2, numBogie);
                }
                else
                {
                    // Since the string content is BOGIE, Int32.TryParse(matrixName.Substring(5), out id) is not needed since its sole purpose is to
                    //  parse the string number from the string.
                    var id = 1;
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43, matrix, id, matrixName.ToString(), numBogie1, numBogie2, numBogie);
                }
                // Bogies contain wheels!
                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i);
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
            else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
            {
                Trace.TraceInformation("Pantrograph matrix with unusual name {1} in shape {0}", TrainCarShape.SharedShape.FilePath, matrixName);
                if (matrixName.Contains("1"))
                    Pantograph1.AddMatrix(matrix);
                else if (matrixName.Contains("2"))
                    Pantograph2.AddMatrix(matrix);
                else
                {
                    if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                    else Pantograph2.AddMatrix(matrix);
                }
            }
            else
            {
                if (matrixAnimated && matrix != 0)
                    RunningGear.AddMatrix(matrix);

                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i);
            }
        }

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommands.ControlPantograph1, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 1, !MSTSWagon.Pantographs[1].CommandUp) });
            UserInputCommands.Add(UserCommands.ControlPantograph2, new Action[] { Noop, () => new PantographCommand(Viewer.Log, 2, !MSTSWagon.Pantographs[2].CommandUp) });
            UserInputCommands.Add(UserCommands.ControlDoorLeft, new Action[] { Noop, () => new ToggleDoorsLeftCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlDoorRight, new Action[] { Noop, () => new ToggleDoorsRightCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlMirror, new Action[] { Noop, () => new ToggleMirrorsCommand(Viewer.Log) });
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
            LeftDoor.UpdateState(MSTSWagon.DoorLeftOpen, elapsedTime);
            RightDoor.UpdateState(MSTSWagon.DoorRightOpen, elapsedTime);
            Mirrors.UpdateState(MSTSWagon.MirrorOpen, elapsedTime);
            UpdateAnimation(frame, elapsedTime);
        }


        private void UpdateAnimation(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float distanceTravelledM;
            if (MSTSWagon.IsDriveable && MSTSWagon.Simulator.UseAdvancedAdhesion)
                //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                //                                distanceTravelledM = MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
                distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
            else
                distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;

            // Running gear animation
            if (!RunningGear.Empty() && MSTSWagon.DriverWheelRadiusM > 0.001)
                RunningGear.UpdateLoop(distanceTravelledM / MathHelper.TwoPi / MSTSWagon.DriverWheelRadiusM);

            // Wheel animation
            if (WheelPartIndexes.Count > 0)
            {
                var wheelCircumferenceM = MathHelper.TwoPi * MSTSWagon.WheelRadiusM;
                var rotationalDistanceR = MathHelper.TwoPi * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR = MathHelper.WrapAngle(WheelRotationR - rotationalDistanceR);
                var wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (var iMatrix in WheelPartIndexes)
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
            }

            // truck angle animation
            foreach (var p in Car.Parts)
            {
                if (p.iMatrix <= 0)
                    continue;
                Matrix m = Matrix.Identity;
                m.Translation = TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;
                m.M11 = p.Cos;
                m.M13 = p.Sin;
                m.M31 = -p.Sin;
                m.M33 = p.Cos;

                //if car vibrate, the bogie will stay on track, thus reverse it back (Car.SuperElevationMatrix holds the inverse)
                if ((Program.Simulator.CarVibrating > 0 || (this.Car.Train != null && this.Car.Train.tilted)) && p.bogie) TrainCarShape.XNAMatrices[p.iMatrix] = Car.SuperElevationMatrix * m;
                else TrainCarShape.XNAMatrices[p.iMatrix] = m;
            }

            if (FreightShape != null)
            {
                if (Viewer.Camera == Viewer.CabCamera && Car == Viewer.CabCamera.AttachedCar)
                {
                    FreightShape.Location.XNAMatrix = Car.GetXNAMatrix();
                }
                else FreightShape.Location.XNAMatrix = Car.WorldPosition.XNAMatrix;
                FreightShape.Location.TileX = Car.WorldPosition.TileX; FreightShape.Location.TileZ = Car.WorldPosition.TileZ;

                if (MSTSWagon.IsTender)
                {
                    if (MSTSWagon.TendersSteamLocomotive == null)
                        MSTSWagon.FindTendersSteamLocomotive();
                    if (FreightShape.XNAMatrices.Length > 0 && MSTSWagon.TendersSteamLocomotive != null)
                        FreightShape.XNAMatrices[0].M42 = MSTSWagon.FreightAnimMinLevelM + MSTSWagon.TendersSteamLocomotive.FuelController.CurrentValue * (MSTSWagon.FreightAnimMaxLevelM - MSTSWagon.FreightAnimMinLevelM);
                }
                FreightShape.PrepareFrame(frame, elapsedTime);
            }

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.Style == ORTS.Viewer3D.Camera.Styles.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime);
            }
            else
            {
                // Skip drawing if CAB view - draw 2D view instead - by GeorgeS
                if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                    Viewer.Camera.Style == ORTS.Viewer3D.Camera.Styles.Cab)
                    return;

                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime);
            }

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

        internal override void Mark()
        {
            TrainCarShape.Mark();
            if (FreightShape != null)
                FreightShape.Mark();
            if (InteriorShape != null)
                InteriorShape.Mark();
        }
    }
}
