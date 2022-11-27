// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* DIESEL LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer.  The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using System.Diagnostics;
using System;
using System.IO;
using System.Text;
using Event = Orts.Common.Event;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks
{
    public class MSTSControlTrailerCar : MSTSLocomotive
    {

        public MSTSControlTrailerCar(Simulator simulator, string wagFile)
    : base(simulator, wagFile)
        {

            PowerSupply = new ScriptedControlCarPowerSupply(this);

        }

        public override void LoadFromWagFile(string wagFilePath)
        {
            base.LoadFromWagFile(wagFilePath);

            Trace.TraceInformation("Control Trailer");

        }


        public override void Initialize()
        {
            
            base.Initialize();

            
        }


        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowerondelay":
                case "engine(ortsauxpowerondelay":
                case "engine(ortspowersupply":
                case "engine(ortstractioncutoffrelay":
                case "engine(ortstractioncutoffrelayclosingdelay":
                case "engine(ortsbattery(mode":
                case "engine(ortsbattery(delay":
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                case "engine(ortselectrictrainsupply(mode":
                case "engine(ortselectrictrainsupply(dieselengineminrpm":
                    LocomotivePowerSupply.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf); break;
            }

        }

        /// <summary>
        /// Set starting conditions  when initial speed > 0 
        /// 

        public override void InitializeMoving()
        {
            base.InitializeMoving();
            WheelSpeedMpS = SpeedMpS;

            ThrottleController.SetValue(Train.MUThrottlePercent / 100);
        }

        /// <summary>
        /// This function updates periodically the states and physical variables of the locomotive's subsystems.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);
            WheelSpeedMpS = SpeedMpS; // Set wheel speed for control car, required to make wheels go around.

        }

        /// <summary>
        /// This function updates periodically the locomotive's motive force.
        /// </summary>
        protected override void UpdateTractiveForce(float elapsedClockSeconds, float t, float AbsSpeedMpS, float AbsWheelSpeedMpS)
        {




        }


        /// <summary>
        /// This function updates periodically the locomotive's sound variables.
        /// </summary>
        protected override void UpdateSoundVariables(float elapsedClockSeconds)
        {



        }







    }
}
