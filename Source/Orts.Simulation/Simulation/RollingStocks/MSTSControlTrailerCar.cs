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

using System.Diagnostics;
using System.IO;
using System.Text;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;

namespace Orts.Simulation.RollingStocks
{
    public class MSTSControlTrailerCar : MSTSLocomotive
    {
        public int ControllerNumberOfGears = 1;
        bool HasGearController = false;
        bool ControlGearUp = false;
        bool ControlGearDown = false;
        int ControlGearIndex;
        int ControlGearIndication;
        TypesGearBox ControlGearBoxType;

        public MSTSControlTrailerCar(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerSupply = new ScriptedControlCarPowerSupply(this);
        }

        public override void LoadFromWagFile(string wagFilePath)
        {
            base.LoadFromWagFile(wagFilePath);
        }

        public override void Initialize()
        {
            // Initialise gearbox controller
            if (ControllerNumberOfGears > 0)
            {
                GearBoxController = new MSTSNotchController(ControllerNumberOfGears + 1);
                if (Simulator.Settings.VerboseConfigurationMessages)
                    HasGearController = true;
                Trace.TraceInformation("Control Car Gear Controller created");
                ControlGearIndex = 0;
                Train.HasControlCarWithGear = true;
            }

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
                case "engine(ortsbattery(defaulton":
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                case "engine(ortselectrictrainsupply(mode":
                case "engine(ortselectrictrainsupply(dieselengineminrpm":
                    LocomotivePowerSupply.Parse(lowercasetoken, stf);
                    break;

                // to setup gearbox controller
                case "engine(gearboxcontrollernumberofgears": ControllerNumberOfGears = stf.ReadIntBlock(null); break;

                default:
                    base.Parse(lowercasetoken, stf); break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a locomotive already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            MSTSControlTrailerCar locoCopy = (MSTSControlTrailerCar)copy;

            ControllerNumberOfGears = locoCopy.ControllerNumberOfGears;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            ControllerFactory.Save(GearBoxController, outf);
            outf.Write(ControlGearIndication);
            outf.Write(ControlGearIndex);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            ControllerFactory.Restore(GearBoxController, inf);
            ControlGearIndication = inf.ReadInt32();
            ControlGearIndex = inf.ReadInt32();
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

            if (ControllerNumberOfGears > 0 && IsLeadLocomotive() && GearBoxController != null)
            {
                // Pass gearbox command key to other locomotives in train, don't treat the player locomotive in this fashion.
                // This assumes that Contol cars have been "matched" with motor cars. Also return values will be on thebasis of the last motor car in the train.         

                foreach (TrainCar car in Train.Cars)
                {
                    var locog = car as MSTSDieselLocomotive;

                    if (locog != null && car != this && !locog.IsLeadLocomotive() && (ControlGearDown || ControlGearUp))
                    {
                        if (ControlGearUp)
                        {
                            locog.GearBoxController.CurrentNotch = GearBoxController.CurrentNotch;
                            locog.GearBoxController.SetValue((float)locog.GearBoxController.CurrentNotch);

                            locog.ChangeGearUp();                                                        
                        }

                        if (ControlGearDown)
                        {
                            locog.GearBoxController.CurrentNotch = GearBoxController.CurrentNotch;
                            locog.GearBoxController.SetValue((float)locog.GearBoxController.CurrentNotch);

                            locog.ChangeGearDown();                            
                        }
                    }

                    // Read values for the HuD and other requirements, will be based upon the last motorcar
                    if (locog != null)
                    {
                        ControlGearIndex = locog.DieselEngines[0].GearBox.CurrentGearIndex;
                        ControlGearIndication = locog.DieselEngines[0].GearBox.GearIndication;
                        if (locog.DieselEngines[0].GearBox.GearBoxType == TypesGearBox.C)
                        {
                            ControlGearBoxType = TypesGearBox.C;
                        }
                    }
                }

                // Rest gear flags once all the cars have been processed
                ControlGearUp = false;
                ControlGearDown = false;
            }
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();
            if (HasGearController)
                status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Gear"),
                ControlGearIndex < 0 ? Simulator.Catalog.GetParticularString("Gear", "N") : (ControlGearIndication).ToString());
            status.AppendLine();

            return status.ToString();
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

        public override void ChangeGearUp()
        {
            if (ControlGearBoxType == TypesGearBox.C)
            {
                if (ThrottlePercent == 0)
                {
                    GearBoxController.CurrentNotch += 1;
                }
                else
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Throttle must be reduced to Idle before gear change can happen."));
                }
            }
            else
            {
                GearBoxController.CurrentNotch += 1;
            }

            if (GearBoxController.CurrentNotch > ControllerNumberOfGears)
            {
                GearBoxController.CurrentNotch = ControllerNumberOfGears;
            }
            else if (GearBoxController.CurrentNotch < 0)
            {
                GearBoxController.CurrentNotch = 0;
            }

            ControlGearUp = true;
            ControlGearDown = false;
        }

        public override void ChangeGearDown()
        {
            if (ControlGearBoxType == TypesGearBox.C)
            {
                if (ThrottlePercent == 0)
                {
                    GearBoxController.CurrentNotch -= 1;
                }
                else
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Throttle must be reduced to Idle before gear change can happen."));
                }
            }
            else
            {
                GearBoxController.CurrentNotch -= 1;
            }

            if (GearBoxController.CurrentNotch > ControllerNumberOfGears)
            {
                GearBoxController.CurrentNotch = ControllerNumberOfGears;
            }
            else if (GearBoxController.CurrentNotch < 0)
            {
                GearBoxController.CurrentNotch = 0;
            }

            ControlGearUp = false;
            ControlGearDown = true;
        }
    }
}
