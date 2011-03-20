/* ELECTRIC LOCOMOTIVE CLASSES
 * 
 * The locomotive is represented by two classes:
 *  ...Simulator - defines the behaviour, ie physics, motion, power generated etc
 *  ...Viewer - defines the appearance in a 3D viewer
 * 
 * The ElectricLocomotive classes add to the basic behaviour provided by:
 *  LocomotiveSimulator - provides for movement, throttle controls, direction controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
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
using MSTS;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.IO;

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph control to the basic LocomotiveSimulator functionality
    /// </summary>
    public class MSTSElectricLocomotive: MSTSLocomotive
    {
        public bool Pan = false;     // false = down;

		public MSTSElectricLocomotive(Simulator simulator, string wagFile, TrainCar previousCar)
			: base(simulator, wagFile, previousCar)
        {
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                // for example
                //case "engine(sound": CabSoundFileName = stf.ReadStringBlock(); break;
                //case "engine(cabview": CVFFileName = stf.ReadStringBlock(); break;
                default: base.Parse(lowercasetoken, stf); break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void InitializeFromCopy(MSTSWagon copy)
        {
            // for example
            //CabSoundFileName = locoCopy.CabSoundFileName;
            //CVFFileName = locoCopy.CVFFileName;

            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(Pan);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            if (inf.ReadBoolean()) SignalEvent(EventID.PantographUp);
            base.Restore(inf);
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSElectricLocomotiveViewer(viewer, this);
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);
            Variable2 = Variable1;
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent( EventID eventID)
        {
            // Modified according to replacable IDs - by GeorgeS
            //switch (eventID)
            do
            {
                if (eventID == EventID.PantographUp) { Pan = true; break; }  // pan up
                if (eventID == EventID.PantographDown) { Pan = false; break; } // pan down
                if (eventID == EventID.PantographToggle) { Pan = !Pan; break; } // pan toggle
            } while (false) ;
            base.SignalEvent(eventID);
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.LINE_VOLTAGE:
                    {
                        if (Pan)
                        {
                            data = (float)Program.Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
                            if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                                data /= 1000;
                        }
                        else
                            data = 0;
                        break;
                    }
                case CABViewControlTypes.PANTOGRAPH:
                case CABViewControlTypes.PANTO_DISPLAY:
                    {
                        data = Pan ? 1 : 0;
                        break;
                    }
                default:
                    {
                        data = base.GetDataOf(cvc);
                        break;
                    }
            }

            return data;
        }

    } // class ElectricLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph animation to the basic LocomotiveViewer class
    /// </summary>
    public class MSTSElectricLocomotiveViewer : MSTSLocomotiveViewer
    {
        MSTSElectricLocomotive ElectricLocomotive;

        List<int> PantographPartIndexes = new List<int>();  // these index into a matrix in the shape file

        float PanAnimationKey = 0;

        public MSTSElectricLocomotiveViewer(Viewer3D viewer, MSTSElectricLocomotive car)
            : base(viewer, car)
        {
            ElectricLocomotive = car;

            // Find the animated parts
            if (TrainCarShape.SharedShape.Animations != null) // skip if the file doesn't contain proper animations
            {
                for (int iMatrix = 0; iMatrix < TrainCarShape.SharedShape.MatrixNames.Count; ++iMatrix)
                {
                    string matrixName = TrainCarShape.SharedShape.MatrixNames[iMatrix].ToUpper();
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
                        case "PANTOGRAPHBOTTOM2":
                        case "PANTOGRAPHBOTTOM2A":
                        case "PANTOGRAPHBOTTOM2B":
                        case "PANTOGRAPHMIDDLE2":
                        case "PANTOGRAPHMIDDLE2A":
                        case "PANTOGRAPHMIDDLE2B":
                        case "PANTOGRAPHTOP2":
                        case "PANTOGRAPHTOP2A":
                        case "PANTOGRAPHTOP2B":
                            PantographPartIndexes.Add(iMatrix);
                            break;
                    }
                }
            }

            // Initialize position based on pan setting ,ie if attaching to a car with the pan up.
            PanAnimationKey = car.Pan ? TrainCarShape.SharedShape.Animations[0].FrameCount : 0;
            foreach (int iMatrix in PantographPartIndexes)
                TrainCarShape.AnimateMatrix(iMatrix, PanAnimationKey);
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Pantograph
            if (UserInput.IsPressed(UserCommands.ControlPantograph))
                ElectricLocomotive.Train.SignalEvent(ElectricLocomotive.Pan ? EventID.PantographDown : EventID.PantographUp);

            base.HandleUserInput( elapsedTime);
        }


        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Pan Animation
            if (PantographPartIndexes.Count > 0)  // skip this if there are no pantographs
            {
                if (ElectricLocomotive.Pan)  // up
                {
                    if (PanAnimationKey < TrainCarShape.SharedShape.Animations[0].FrameCount)
                    {
                        // moving up
                        PanAnimationKey += 2f * elapsedTime.ClockSeconds;
                        if (PanAnimationKey > TrainCarShape.SharedShape.Animations[0].FrameCount) PanAnimationKey = TrainCarShape.SharedShape.Animations[0].FrameCount;
                        foreach (int iMatrix in PantographPartIndexes)
                            TrainCarShape.AnimateMatrix(iMatrix, PanAnimationKey);
                    }
                }
                else // down
                {
                    if (PanAnimationKey > 0)
                    {
                        // moving down
                        PanAnimationKey -= 2f * elapsedTime.ClockSeconds;
                        if (PanAnimationKey < 0) PanAnimationKey = 0;
                        foreach (int iMatrix in PantographPartIndexes)
                            TrainCarShape.AnimateMatrix(iMatrix, PanAnimationKey);
                    }
                }
            }
            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }


    } // class ElectricLocomotiveViewer

}
