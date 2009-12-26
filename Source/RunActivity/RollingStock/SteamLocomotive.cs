/* STEAM LOCOMOTIVE CLASSES
 * 
 * The steam locomotive is represented by two classes:
 *  SteamLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  SteamLocomotiveViewer - defines the appearance in a 3D viewer
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
    /// Adds physics and control for a steam locomotive
    /// </summary>
    public class SteamLocomotive: Locomotive
    {
        public SteamLocomotive(WAGFile wagFile)
            : base(wagFile)
        {
        }

        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new SteamLocomotiveViewer( viewer, this );
        }

        public override void Update( float elapsedClockSeconds )
        {            
            base.Update( elapsedClockSeconds);

            Variable1 = Math.Abs(SpeedMpS);
        }
    } // class SteamLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special steam loco animation to the basic LocomotiveViewer class
    /// </summary>
    class SteamLocomotiveViewer : LocomotiveViewer
    {
        SteamLocomotive SteamLocomotive;

        public SteamLocomotiveViewer(Viewer3D viewer, SteamLocomotive car)
            : base(viewer, car)
        {
            SteamLocomotive = car;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput( elapsedTime);
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            base.Update(elapsedTime);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame( frame, elapsedTime);
        }

        public override void Unload()
        {
            base.Unload();
        }

    } // class SteamLocomotiveViewer

}
