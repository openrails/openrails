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
        // simulation parameters
        public float Variable1 = 0.0f;  // used to convey status to soundsource
        public float Variable2 = 0.0f;
        public float Variable3 = 0.0f;

        // wag file data
        public string MainShapeFileName = null;
        public string FreightShapeFileName = null;
        public string InteriorShapeFileName = null; // passenger view shape file name
        public string MainSoundFileName = null;
        public string InteriorSoundFileName = null;
        public float WheelRadiusM = 1;          // provide some defaults in case its missing from the wag
        public float DriverWheelRadiusM = 1.5f;    // provide some defaults in case its missing from the wag

        public MSTSBrakeSystem MSTSBrakeSystem { get { return (MSTSBrakeSystem)base.BrakeSystem; } }

        public MSTSWagon(string wagFilePath): base( wagFilePath )
        {
            if (CarManager.LoadedCars.ContainsKey(wagFilePath))
            {
                InitializeFromCopy(CarManager.LoadedCars[wagFilePath]);
            }
            else
            {
                InitializeFromWagFile(wagFilePath);
                CarManager.LoadedCars.Add(wagFilePath, this);
            }
        }

        /// <summary>
        /// This initializer is called when we haven't loaded this type of car before
        /// and must read it new from the wag file.
        /// </summary>
        public virtual void InitializeFromWagFile(string wagFilePath)
        {
            STFReader f = new STFReader(wagFilePath);
            while (!f.EOF())
            {
                string token = f.ReadToken();
                if (token == ")")
                    Parse(f.Tree.ToLower() + ")", f);  // ie  wagon(inside) at end of block
                else
                    Parse(f.Tree.ToLower(), f);  // otherwise wagon(inside
            }
            f.Close();
        }

        ViewPoint passengerViewPoint = new ViewPoint();

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public virtual void Parse(string lowercasetoken, STFReader f)
        {

            switch (lowercasetoken)
            {
                case "wagon(wagonshape": MainShapeFileName = f.ReadStringBlock(); break;
                case "wagon(freightanim": f.VerifyStartOfBlock(); FreightShapeFileName = f.ReadToken(); f.SkipRestOfBlock(); break; // TODO complete parse
                case "wagon(size": f.VerifyStartOfBlock(); f.ReadFloat(); f.ReadFloat(); Length = f.ReadFloat(); f.VerifyEndOfBlock(); break;
                case "wagon(mass": MassKG = f.ReadFloatBlock(); break;
                case "wagon(inside(sound": InteriorSoundFileName = f.ReadStringBlock(); break;
                case "wagon(inside(passengercabinfile": InteriorShapeFileName = f.ReadStringBlock(); break;
                case "wagon(inside(passengercabinheadpos": passengerViewPoint.Location = f.ReadVector3Block(); break;
                case "wagon(inside(rotationlimit": passengerViewPoint.RotationLimit = f.ReadVector3Block(); break;
                case "wagon(inside(startdirection": passengerViewPoint.StartDirection = f.ReadVector3Block(); break;
                case "wagon(inside)": PassengerViewpoints.Add(passengerViewPoint); break;
                case "wagon(wheelradius": WheelRadiusM = f.ReadFloatBlock(); break;
                case "engine(wheelradius": DriverWheelRadiusM = f.ReadFloatBlock(); break;
                case "wagon(sound": MainSoundFileName = f.ReadStringBlock(); break;
                case "wagon(brakesystemtype": string braketype = f.ReadStringBlock();
                    switch (braketype.ToLower()) {
                        case "air_single_pipe": BrakeSystem = new AirSinglePipe(this); break; //TODO parse and setup other brake systems
                        default: BrakeSystem = new AirSinglePipe(this); break;
                    } break;
            }

            if (MSTSBrakeSystem != null)
                MSTSBrakeSystem.Parse(lowercasetoken, f);
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// 
        /// IMPORTANT NOTE:  everything you initialized in parse, must be initialized here
        /// </summary>
        public virtual void InitializeFromCopy(MSTSWagon copy)
        {
            MainShapeFileName = copy.MainShapeFileName;
            FreightShapeFileName = copy.FreightShapeFileName;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            MainSoundFileName = copy.MainSoundFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            Length = copy.Length;
            MassKG = copy.MassKG;
            foreach (ViewPoint passengerViewPoint in copy.PassengerViewpoints)
                PassengerViewpoints.Add(passengerViewPoint);
            foreach (ViewPoint frontCabViewPoint in copy.FrontCabViewpoints)
                FrontCabViewpoints.Add(frontCabViewPoint);
            foreach (ViewPoint rearCabViewPoint in copy.RearCabViewpoints)
                RearCabViewpoints.Add(rearCabViewPoint);

            BrakeSystem = new AirSinglePipe(this);  // TODO - select different types
            MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(Variable1);
            outf.Write(Variable2);
            outf.Write(Variable3);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            Variable1 = inf.ReadSingle();
            Variable2 = inf.ReadSingle();
            Variable3 = inf.ReadSingle();
            base.Restore(inf);
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

            MSTSBrakeSystem.Update(elapsedClockSeconds);
        }


        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(EventID eventID)
        {
            foreach (CarEventHandler eventHandler in EventHandlers)
                eventHandler.HandleCarEvent(eventID);
        }

        // sound sources or and viewers can register them selves to get direct notification of an event
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
        protected AnimatedShape FreightShape = null;
        protected AnimatedShape InteriorShape = null;
        protected List<SoundSource> SoundSources = new List<SoundSource>();

        List<int> WheelPartIndexes = new List<int>();   // these index into a matrix in the shape file
        List<int> RunningGearPartIndexes = new List<int>();

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon) Car; } }

        public MSTSWagonViewer(Viewer3D viewer, MSTSWagon car): base( viewer, car )
        {
            string wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";
            string shapePath = wagonFolderSlash + car.MainShapeFileName;

            TrainCarShape = new PoseableShape(viewer, shapePath, car.WorldPosition);
            if (car.FreightShapeFileName != null)
            {
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName, car.WorldPosition);
            }
            if (car.InteriorShapeFileName != null)
            {
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName, car.WorldPosition);
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
            if (RunningGearPartIndexes.Count > 0 && MSTSWagon.DriverWheelRadiusM > 0.001 )  // skip this if there is no running gear and only engines can have running gear
            {
                float driverWheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.DriverWheelRadiusM;
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
                float wheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.WheelRadiusM;
                float rotationalDistanceR = 3.14159f * 2.0f * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR -= rotationalDistanceR;
                while (WheelRotationR > Math.PI) WheelRotationR -= (float)Math.PI * 2;   // normalize for -180 to +180 degrees
                while (WheelRotationR < -Math.PI) WheelRotationR += (float)Math.PI * 2;
                Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (int iMatrix in WheelPartIndexes)
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
            }

            if (FreightShape != null)
                FreightShape.PrepareFrame(frame, elapsedTime.ClockSeconds);

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedToCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.ViewPoint == Camera.ViewPoints.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime.ClockSeconds);
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
            if( MSTSWagon.MainSoundFileName != null ) LoadCarSound(wagonFolderSlash, MSTSWagon.MainSoundFileName );
            if (MSTSWagon.InteriorSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
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


    /// <summary>
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }


} // namespace ORTS
