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

        public MSTSElectricLocomotive(string wagFile)
            : base( wagFile)
        {
        }

        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSElectricLocomotiveViewer(viewer, this);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent( EventID eventID)
        {
            switch (eventID)
            {
                case EventID.PantographUp: Pan = true; break;  // pan up
                case EventID.PantographDown : Pan = false; break; // pan down
                case EventID.PantographToggle: Pan = !Pan; break;  // pan toggle
            }
            base.SignalEvent(eventID);
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
                for (int iMatrix = 0; iMatrix < TrainCarShape.SharedShape.MatrixNames.Length; ++iMatrix)
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
        }

        public override void HandleUserInput( ElapsedTime elapsedTime)
        {
            // Pantograph
            if (UserInput.IsPressed(Keys.P) && !UserInput.IsShiftDown())
                ElectricLocomotive.Train.SignalEvent(ElectricLocomotive.Pan ? EventID.PantographDown : EventID.PantographUp);

            base.HandleUserInput( elapsedTime);
        }


        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Pan Animation
            if (PantographPartIndexes.Count > 0)  // skip this if there are no pantographs
            {
                if (ElectricLocomotive.Pan)  // up
                {
                    if (PanAnimationKey < 0.999)
                    {
                        // moving up
                        PanAnimationKey += 2f * elapsedTime.ClockSeconds;
                        if (PanAnimationKey > 0.999) PanAnimationKey = 1.0f;
                        foreach (int iMatrix in PantographPartIndexes)
                            TrainCarShape.AnimateMatrix(iMatrix, PanAnimationKey);
                    }
                }
                else // down
                {
                    if (PanAnimationKey > 0.001)
                    {
                        // moving down
                        PanAnimationKey -= 2f * elapsedTime.ClockSeconds;
                        if (PanAnimationKey < 0.001) PanAnimationKey = 0;
                        foreach (int iMatrix in PantographPartIndexes)
                            TrainCarShape.AnimateMatrix(iMatrix, PanAnimationKey);
                    }
                }
            }
            base.PrepareFrame(frame, elapsedTime);
        }

        public override void Unload()
        {
            base.Unload();
        }


    } // class ElectricLocomotiveViewer

}
