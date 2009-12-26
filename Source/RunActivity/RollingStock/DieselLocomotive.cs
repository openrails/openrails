/* DIESEL LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  DieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  DieselLocomotiveViewer - defines the appearance in a 3D viewer
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
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
    /// Adds physics and control for a diesel locomotive
    /// </summary>
    public class DieselLocomotive : Locomotive
    {
        public DieselLocomotive(WAGFile wagFile)
            : base(wagFile)
        {
        }

        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new DieselLocomotiveViewer(viewer, this);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds );
        }

    } // class DieselLocomotive


    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special Diesel loco animation to the basic LocomotiveViewer class
    /// </summary>
    class DieselLocomotiveViewer : LocomotiveViewer
    {
        DieselLocomotive DieselLocomotive;

        public DieselLocomotiveViewer(Viewer3D viewer, DieselLocomotive car)
            : base(viewer, car)
        {
            DieselLocomotive = car;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
        }

        public override void Unload()
        {
            base.Unload();
        }

    } // class DieselLocomotiveViewer

}
