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
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special Diesel loco animation to the basic LocomotiveViewer class
    /// </summary>
    class DieselLocomotiveViewer : LocomotiveViewer
    {
        DieselLocomotiveSimulator DieselLocomotive;

        public DieselLocomotiveViewer(Viewer viewer, DieselLocomotiveSimulator car)
            : base(viewer, car)
        {
            DieselLocomotive = car;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

    } // class DieselLocomotiveViewer

}
