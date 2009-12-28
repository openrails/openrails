/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
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
///   SIMULATION BEHAVIOUR
///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>
    public class MSTSWagon: TrainCar
    {
        public WAGFile WagFile;
        public string WagFilePath;
        public CVFFile CVFFile = null;
        public float Variable1 = 0.0f;  // used to convey status to soundsource
        public float Variable2 = 0.0f;
        public float Variable3 = 0.0f;
     

        public MSTSWagon(string wagFilePath)
        {
            WagFilePath = wagFilePath;
            WagFile = SharedWAGFileManager.Get(wagFilePath);
            Length = WagFile.Wagon.Length;
            MassKG = WagFile.Wagon.MassKG;
        }

        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            // warning - don't assume there is only one viewer, or that there are any viewers at all.
            // Best practice is not to give the TrainCar class any knowledge of its viewers.
            return new MSTSWagonViewer(viewer, this);
        }

        public override void Update( float elapsedClockSeconds )
        {
            if (SpeedMpS < 0.1)
            {
                // Starting Friction 
                //
                //                      Above Freezing   Below Freezing
                //    Journal Bearing      25 lb/ton        35 lb/ton   (short ton)
                //     Roller Bearing       5 lb/ton        15 lb/ton
                //
                // [2009-10-25 from http://www.arema.org/publications/pgre/ ]

                float NpKG = 30f /* lb/ton */ * 4.84e-3f;  // convert lbs/short-ton to N/kg

                FrictionForceN = MassKG * NpKG;

                //FrictionForceN *= 2; // for better playability  // TODO why do we need this?
            }
            else
            {
                // Davis Formula for rolling friction
                float Asqft = 100f; // square feet cross sectional area
                float Wst = 30f / 4f; // short tons per axle weight of the car
                float Vmph = SpeedMpS * 0.000621371192f /* miles/M */ * 3600f /* sec/hr */; // convert speed to mph
                float N = 4; // number of axles
                float RlbPst; // resistance in lbs per ton

                // for friction bearings
                RlbPst = 1.3f + 29f / Wst + 0.045f * Vmph + 0.0005f * Asqft * Vmph * Vmph / (Wst * N);

                // for roller bearings
                // R = 0.6f + 20f / W + 0.01f * V + 0.07f * A * V * V / (W * N);

                float NpKG = RlbPst * 4.84e-3f;  // convert lbs/short-ton to N/kg

                FrictionForceN = MassKG * NpKG;

                //FrictionForceN *= 3; // for better playability  // TODO why do we need this
            }

            // Typical brake shoe force = 20,000 pounds or 89,000 newtons
            FrictionForceN += 89e3f * Train.TrainBrakePercent / 100f; 

            // TODO add static friction effect

            // TODO compute gravity as 'motive force'
        }

        public override void CreateEvent(int eventID)
        {
            foreach (CarEventHandler eventHandler in EventHandlers)
                eventHandler.HandleCarEvent(eventID);
        }

        public List<CarEventHandler> EventHandlers = new List<CarEventHandler>();

    }

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Note:  we need a separate viewer class since there could be multiple viewers
    /// for a single traincar, or possibly none
    /// </summary>

    public class MSTSWagonViewer: TrainCarViewer
    {
        protected float WheelRotationR = 0f;  // radians track rolling of wheels
        float DriverRotationKey;  // advances animation with the driver rotation

        protected PoseableShape TrainCarShape = null;
        protected AnimatedShape FreightAnimShape = null;
        protected AnimatedShape PassengerCabin = null;
        protected List<SoundSource> SoundSources = new List<SoundSource>();

        List<int> WheelPartIndexes = new List<int>();   // these index into a matrix in the shape file
        List<int> RunningGearPartIndexes = new List<int>();

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon) Car; } }

        public MSTSWagonViewer(Viewer3D viewer, MSTSWagon car): base( viewer, car )
        {
            string wagonFolderSlash = viewer.Simulator.BasePath + @"\TRAINS\TRAINSET\" + car.WagFile.Folder + @"\";
            string shapePath = wagonFolderSlash + car.WagFile.Wagon.WagonShape;

            TrainCarShape = new PoseableShape(viewer, shapePath, car.WorldPosition);
            if (car.WagFile.Wagon.FreightAnim != null)
            {
                FreightAnimShape = new AnimatedShape(viewer, wagonFolderSlash + car.WagFile.Wagon.FreightAnim, car.WorldPosition);
            }
            if (car.WagFile.Wagon.Inside != null)
            {
                PassengerCabin = new AnimatedShape(viewer, wagonFolderSlash + car.WagFile.Wagon.Inside.PassengerCabinFile, car.WorldPosition);
            }

            LoadCarSounds(wagonFolderSlash);
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
                                && !matrixName.StartsWith("DOOR_")) // don't want to animate every object
                            if (TrainCarShape.SharedShape.Animations != null
                                && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
                                && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0)  // ensure shape file is setup properly
                                RunningGearPartIndexes.Add(iMatrix);
                        break;
                }
            }


        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
        }


        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            UpdateSound(elapsedTime);
            UpdateAnimation(frame, elapsedTime);
        }


        public void UpdateSound(ElapsedTime elapsedTime)
        {
            try
            {
                foreach (SoundSource soundSource in SoundSources)
                    soundSource.Update(elapsedTime);
            }
            catch( System.Exception error )
            {
                Console.Error.WriteLine("Updating Sound: " + error.Message);
            }
        }


        private void UpdateAnimation( RenderFrame frame, ElapsedTime elapsedTime )
        {
            float distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds ;

            // Running gear animation
            if (RunningGearPartIndexes.Count > 0 && MSTSWagon.WagFile.Engine != null)  // skip this if there is no running gear and only engines can have running gear
            {
                float driverWheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.WagFile.Engine.DriverWheelRadiusM;
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
                float wheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.WagFile.Wagon.WheelRadiusM;
                float rotationalDistanceR = 3.14159f * 2.0f * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR -= rotationalDistanceR;
                while (WheelRotationR > Math.PI) WheelRotationR -= (float)Math.PI * 2;   // normalize for -180 to +180 degrees
                while (WheelRotationR < -Math.PI) WheelRotationR += (float)Math.PI * 2;
                Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (int iMatrix in WheelPartIndexes)
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
            }

            if (FreightAnimShape != null)
                FreightAnimShape.PrepareFrame(frame, elapsedTime.ClockSeconds);

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedToCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.ViewPoint == Camera.ViewPoints.Passenger)
            {
                // We are in the passenger cabin
                if (PassengerCabin != null)
                    PassengerCabin.PrepareFrame(frame, elapsedTime.ClockSeconds);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime.ClockSeconds);
            }
            else
            {
                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime.ClockSeconds);
            }

        }



        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public virtual void Unload()
        {
            SoundSources.Clear();
        }


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        private void LoadCarSounds(string wagonFolderSlash)
        {
            LoadCarSound(wagonFolderSlash, MSTSWagon.WagFile.Wagon.Sound);
            if (MSTSWagon.WagFile.Wagon.Inside != null) LoadCarSound(wagonFolderSlash, MSTSWagon.WagFile.Wagon.Inside.Sound);
            if (MSTSWagon.WagFile.Engine != null) LoadCarSound(wagonFolderSlash, MSTSWagon.WagFile.Engine.Sound);
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

            SoundSources.Add(new SoundSource(Viewer, MSTSWagon, smsFilePath));
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
            SoundSources.Add(new SoundSource(Viewer, MSTSWagon, path));
        }

    } // class carshape



} // namespace ORTS
