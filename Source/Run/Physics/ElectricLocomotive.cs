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
    public class ElectricLocomotiveSimulator: LocomotiveSimulator
    {
        public bool Pan = false;     // false = down;

        public ElectricLocomotiveSimulator(WAGFile wagFile)
            : base(wagFile)
        {
        }

        public override void HandleKeyboard(KeyboardInput keyboard, GameTime gameTime)
        {
            // Pantograph
            if (keyboard.IsPressed(Keys.P))
            {
                Pan = !Pan;
                CreateEvent(47); // pantograph toggle 
                CreateEvent(Pan ? 45 : 46);  // up or down event
            }

            base.HandleKeyboard(keyboard, gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    } // class ElectricLocomotiveSimulator


}
