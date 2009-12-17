/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.IO;
using MSTS;

namespace ORTS
{


///////////////////////////////////////////////////
///   3D VIEW
///////////////////////////////////////////////////

    
    public class TrainCarViewer: GameComponent 
    {
        protected TrainCarSimulator Car;
        protected float WheelRotationR = 0f;  // radians track rolling of wheels
        float DriverRotationKey;  // advances animation with the driver rotation

        protected PoseableShape TrainCarShape = null;
        protected AnimatedShape FreightAnimShape = null;
        protected AnimatedShape PassengerCabin = null;
        protected List<SoundSource> SoundSources = new List<SoundSource>();

        List<int> WheelPartIndexes = new List<int>();   // these index into a matrix in the shape file
        List<int> RunningGearPartIndexes = new List<int>();

        protected Viewer Viewer;

        public TrainCarViewer(Viewer viewer, TrainCarSimulator car)
            : base(viewer )
        {
            Car = car;
            Viewer = viewer;

            string wagonFolderSlash = viewer.Simulator.BasePath + @"\TRAINS\TRAINSET\" + car.WagFile.Folder + @"\";
            string shapePath = wagonFolderSlash + car.WagFile.Wagon.WagonShape;

            TrainCarShape = new PoseableShape(viewer, shapePath, car.WorldPosition);
            viewer.Components.Add(TrainCarShape);
            if (car.WagFile.Wagon.FreightAnim != null)
            {
                FreightAnimShape = new AnimatedShape(viewer, wagonFolderSlash + car.WagFile.Wagon.FreightAnim, car.WorldPosition);
                viewer.Components.Add(FreightAnimShape);
            }
            if (car.WagFile.Wagon.Inside != null)
            {
                PassengerCabin = new AnimatedShape(viewer, wagonFolderSlash + car.WagFile.Wagon.Inside.PassengerCabinFile, car.WorldPosition);
                viewer.Components.Add(PassengerCabin);
            }

            LoadCarSounds( wagonFolderSlash);
            LoadTrackSounds();

            // Get indexes of all the animated parts
            for (int iMatrix = 0; iMatrix < TrainCarShape.SharedShape.MatrixNames.Length; ++iMatrix)
            {
                string matrixName = TrainCarShape.SharedShape.MatrixNames[iMatrix].ToUpper();
                switch (matrixName)
                {
                    case "WHEELS11":
                    case "WHEELS12":
                    case "WHEELS13":
                    case "WHEELS21":
                    case "WHEELS22":
                    case "WHEELS23":
                        WheelPartIndexes.Add(iMatrix);
                        break;
                    case "BOGIE1":
                    case "BOGIE2":
                        // BOGIES - TODO
                        break;
                    default: if (!matrixName.StartsWith("PANTOGRAPH") 
                                && !matrixName.StartsWith("WIPER") 
                                && !matrixName.StartsWith("MIRROR")
                                && !matrixName.StartsWith("DOOR_") ) // don't want to animate every object
                        if (TrainCarShape.SharedShape.Animations != null 
                            && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
                            && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0 )  // ensure shape file is setup properly
                            RunningGearPartIndexes.Add(iMatrix);
                        break;
                }
            }


        }


        public override void Update(GameTime gameTime)
        {
            // Control visibility of passenger cabin when inside it
            if (PassengerCabin != null)
            {
                if (Viewer.Camera.AttachedToCar == this.Car
                     && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                         Viewer.Camera.ViewPoint == Camera.ViewPoints.Passenger)
                {
                    // We are in the passenger cabin
                    PassengerCabin.Visible = true;
                    PassengerCabin.Enabled = true;
                    TrainCarShape.Visible = false;
                }
                else
                {
                    // We are outside the passenger cabin
                    PassengerCabin.Visible = false;
                    PassengerCabin.Enabled = false;
                    TrainCarShape.Visible = true;
                }
            }

            if (!Viewer.Simulator.Paused)
            {
                float distanceTravelledM = Car.SpeedMpS * (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Running gear animation
                if (RunningGearPartIndexes.Count > 0 && Car.WagFile.Engine != null)  // skip this if there is no running gear and only engines can have running gear
                {
                    float driverWheelCircumferenceM = 3.14159f * 2.0f * Car.WagFile.Engine.DriverWheelRadiusM;
                    float framesAdvanced = (float)TrainCarShape.SharedShape.Animations[0].FrameCount * distanceTravelledM / driverWheelCircumferenceM;
                    DriverRotationKey += framesAdvanced;  // ie, with 8 frames of animation, the key will advance from 0 to 8 at the specified speed.
                    while (DriverRotationKey >= TrainCarShape.SharedShape.Animations[0].FrameCount) DriverRotationKey -= TrainCarShape.SharedShape.Animations[0].FrameCount;
                    while (DriverRotationKey < -0.00001) DriverRotationKey += TrainCarShape.SharedShape.Animations[0].FrameCount;
                    foreach (int iMatrix in RunningGearPartIndexes)
                        TrainCarShape.AnimateMatrix(iMatrix, DriverRotationKey);
                }

                // Wheel animation
                if (WheelPartIndexes.Count > 0)
                {
                    float wheelCircumferenceM = 3.14159f * 2.0f * Car.WagFile.Wagon.WheelRadiusM;
                    float rotationalDistanceR = 3.14159f * 2.0f * distanceTravelledM / wheelCircumferenceM;  // in radians
                    WheelRotationR -= rotationalDistanceR;
                    while (WheelRotationR > Math.PI) WheelRotationR -= (float)Math.PI * 2;   // normalize for -180 to +180 degrees
                    while (WheelRotationR < -Math.PI) WheelRotationR += (float)Math.PI * 2;
                    Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                    foreach (int iMatrix in WheelPartIndexes)
                        TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
                }
            }
        }//public override void Update(GameTime gameTime)


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        private void LoadCarSounds( string wagonFolderSlash )
        {
            LoadCarSound(wagonFolderSlash, Car.WagFile.Wagon.Sound);
            if (Car.WagFile.Wagon.Inside != null) LoadCarSound(wagonFolderSlash, Car.WagFile.Wagon.Inside.Sound);
            if (Car.WagFile.Engine != null) LoadCarSound(wagonFolderSlash, Car.WagFile.Engine.Sound);
        }


        /// <summary>
        /// Load the car sound, attach it to the car
        /// check first in the wagon folder, then the global folder for the sound.
        /// If not found, report a warning.
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        /// <param name="filename"></param>
        private void LoadCarSound(string wagonFolderSlash, string filename)
        {
            if (filename == null)
                return;
            string smsFilePath = wagonFolderSlash + @"sound\" + filename;
            if (!File.Exists(smsFilePath))
                smsFilePath = Viewer.Simulator.BasePath + @"\sound\" + filename;
            if (!File.Exists(smsFilePath))
            {
                Console.Error.WriteLine(wagonFolderSlash + " - can't find " + filename);
                return;
            }

            SoundSources.Add( new SoundSource(Viewer, Car, smsFilePath) );
        }

        /// <summary>
        /// Load the inside and outside sounds for the default level 0 track type.
        /// </summary>
        private void LoadTrackSounds()
        {
            if (Viewer.TTypeDatFile.Count > 0)  // TODO, still have to figure out if this should be part of the car, or train, or track
            {
                LoadTrackSound(Viewer.TTypeDatFile[0].InsideSound);
                LoadTrackSound(Viewer.TTypeDatFile[0].OutsideSound);
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
                Console.Error.WriteLine("ttype.dat - can't find " + filename);
                return;
            }
            SoundSources.Add( new SoundSource(Viewer, Car, path) );
        }

        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public virtual void Unload()
        {
            if (TrainCarShape != null) Viewer.Components.Remove(TrainCarShape);
            if (FreightAnimShape != null) Viewer.Components.Remove(FreightAnimShape);
            if (PassengerCabin != null) Viewer.Components.Remove(PassengerCabin);
            foreach (SoundSource soundSource in SoundSources)
               soundSource.Dispose();
            SoundSources.Clear();
        }

    } // class carshape


} // namespace ORTS
/*

 StaticShape
 PoseableShape
 AnimatedShape
 
 
 
 */