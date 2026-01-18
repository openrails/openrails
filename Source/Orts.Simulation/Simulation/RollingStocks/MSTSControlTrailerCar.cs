// COPYRIGHT 2009 by the Open Rails project.
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

using System.Diagnostics;
using System.IO;
using System.Text;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;

namespace Orts.Simulation.RollingStocks
{
    public class MSTSControlTrailerCar : MSTSLocomotive
    {
        public int ControllerNumberOfGears = 0;
        bool HasGearController = false;
        bool ControlGearUp = false;
        bool ControlGearDown = false;
        int ControlGearIndex;
        int ControlGearIndication;
        TypesGearBox ControlGearBoxType;

        private bool controlTrailerBrakeSystemSet = false;

        public MSTSControlTrailerCar(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerSupply = new ScriptedControlCarPowerSupply(this);
        }

        public override void Initialize(bool reinitialize = false)
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

            base.Initialize(reinitialize);
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowersupply":
                case "engine(ortspowersupplyparameters":
                case "engine(ortsbattery":
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
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
            FindControlActiveLocomotive();
            // A control car typically doesn't have its own compressor and relies on the attached power car. However OR uses the lead locomotive as the reference car for compressor calculations.
            // Hence whilst users are encouraged to leave these parameters out of the ENG file, they need to be setup for OR to work correctly.
            // Some parameters need to be split across the unpowered and powered car for correct timing and volume calculations.
            // This setup loop is only processed the first time that update is run.
            if (!controlTrailerBrakeSystemSet)
            {
                if (ControlActiveLocomotive != null)
                {
                    // Split reservoir volume across the power car and the active locomotive
                    MainResVolumeM3 = ControlActiveLocomotive.MainResVolumeM3 / 2;
                    ControlActiveLocomotive.MainResVolumeM3 = MainResVolumeM3;

                    MaxMainResPressurePSI = ControlActiveLocomotive.MaxMainResPressurePSI;
                    MainResPressurePSI = MaxMainResPressurePSI;
                    ControlActiveLocomotive.MainResPressurePSI = MainResPressurePSI;
                    controlTrailerBrakeSystemSet = true; // Ensure this loop is only processes the first time update routine run
                    MaximumMainReservoirPipePressurePSI = ControlActiveLocomotive.MaximumMainReservoirPipePressurePSI;
                    CompressorRestartPressurePSI = ControlActiveLocomotive.CompressorRestartPressurePSI;
                    MainResChargingRatePSIpS = ControlActiveLocomotive.MainResChargingRatePSIpS;
                    BrakePipeChargingRatePSIorInHgpS = ControlActiveLocomotive.BrakePipeChargingRatePSIorInHgpS;
                    TrainBrakePipeLeakPSIorInHgpS = ControlActiveLocomotive.TrainBrakePipeLeakPSIorInHgpS;
                }
            }

            base.Update(elapsedClockSeconds);
            WheelSpeedMpS = SpeedMpS; // Set wheel speed for control car, required to make wheels go around.

            if (ControllerNumberOfGears > 0 && IsLeadLocomotive() && GearBoxController != null)
            {
                // Pass gearbox command key to other locomotives in train, don't treat the player locomotive in this fashion.
                // This assumes that Contol cars have been "matched" with motor cars. Also return values will be on thebasis of the last motor car in the train.         

                foreach (TrainCar car in Train.Cars)
                {
                    var locog = car as MSTSDieselLocomotive;

                    if (locog != null && locog.DieselEngines[0].GearBox != null && locog.DieselEngines[0].GearBox != null && car != this && !locog.IsLeadLocomotive() && (ControlGearDown || ControlGearUp))
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
                    if (locog != null && locog.DieselEngines[0].GearBox != null && locog.DieselEngines[0].GearBox != null)
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
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Battery switch"),
                LocomotivePowerSupply.BatterySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Master key"),
                LocomotivePowerSupply.MasterKey.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            if (ControlActiveLocomotive != null)
            {
                status.AppendLine();
                if (ControlActiveLocomotive is MSTSElectricLocomotive electric)
                {
                    status.AppendFormat("{0} = ", Simulator.Catalog.GetString("Pantographs"));
                    foreach (var pantograph in electric.Pantographs.List)
                        status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(pantograph.State)));
                    status.AppendLine();
                    status.AppendFormat("{0} = {1}\n",
                        Simulator.Catalog.GetString("Circuit breaker"),
                        Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(electric.ElectricPowerSupply.CircuitBreaker.State)));
                }
                else if (ControlActiveLocomotive is MSTSDieselLocomotive diesel)
                {
                    status.AppendLine();
                    status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Engine"),
                        Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(diesel.DieselEngines[0].State)));
                    if (HasGearController)
                        status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Gear"),
                        ControlGearIndex < 0 ? Simulator.Catalog.GetParticularString("Gear", "N") : (ControlGearIndication).ToString());
                    status.AppendFormat("{0} = {1}\n",
                        Simulator.Catalog.GetString("Traction cut-off relay"),
                        Simulator.Catalog.GetParticularString("TractionCutOffRelay", GetStringAttribute.GetPrettyName(diesel.DieselPowerSupply.TractionCutOffRelay.State)));
                }
                status.AppendFormat("{0} = {1}\n",
                    Simulator.Catalog.GetString("Electric train supply"),
                    ControlActiveLocomotive.LocomotivePowerSupply.ElectricTrainSupplySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
                status.AppendLine();
                status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetParticularString("PowerSupply", "Power"),
                    Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(ControlActiveLocomotive.LocomotivePowerSupply.MainPowerSupplyState)));
            }
            return status.ToString();
        }

        /// <summary>
        /// This function updates periodically the locomotive's motive force.
        /// </summary>
        protected override void UpdateTractiveForce(float elapsedClockSeconds)
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
        public override float GetDataOf(CabViewControl cvc)
        {
            float data;
            switch (cvc.ControlType.Type)
            {
                // Locomotive controls
                case CABViewControlTypes.AMMETER:
                case CABViewControlTypes.AMMETER_ABS:
                case CABViewControlTypes.DYNAMIC_BRAKE_FORCE:
                case CABViewControlTypes.LOAD_METER:
                case CABViewControlTypes.ORTS_SIGNED_TRACTION_BRAKING:
                case CABViewControlTypes.ORTS_SIGNED_TRACTION_TOTAL_BRAKING:
                case CABViewControlTypes.TRACTION_BRAKING:
                case CABViewControlTypes.WHEELSLIP:
                    data = ControlActiveLocomotive?.GetDataOf(cvc) ?? 0;
                    break;
                // Diesel locomotive controls
                case CABViewControlTypes.FUEL_GAUGE:
                case CABViewControlTypes.ORTS_DIESEL_TEMPERATURE:
                case CABViewControlTypes.ORTS_OIL_PRESSURE:
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_AUTHORIZED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_CLOSED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_AUTHORIZATION:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_ORDER:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_OPENING_ORDER:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN_AND_AUTHORIZED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_STATE:
                case CABViewControlTypes.RPM:
                case CABViewControlTypes.RPM_2:
                    data = (ControlActiveLocomotive as MSTSDieselLocomotive)?.GetDataOf(cvc) ?? 0;
                    break;
                // Electric locomotive controls
                case CABViewControlTypes.LINE_VOLTAGE:
                case CABViewControlTypes.ORTS_PANTOGRAPH_VOLTAGE_AC:
                case CABViewControlTypes.ORTS_PANTOGRAPH_VOLTAGE_DC:
                case CABViewControlTypes.PANTO_DISPLAY:
                case CABViewControlTypes.PANTOGRAPH:
                case CABViewControlTypes.PANTOGRAPH2:
                case CABViewControlTypes.ORTS_PANTOGRAPH3:
                case CABViewControlTypes.ORTS_PANTOGRAPH4:
                case CABViewControlTypes.PANTOGRAPHS_4:
                case CABViewControlTypes.PANTOGRAPHS_4C:
                case CABViewControlTypes.PANTOGRAPHS_5:
                case CABViewControlTypes.ORTS_VOLTAGE_SELECTOR:
                case CABViewControlTypes.ORTS_PANTOGRAPH_SELECTOR:
                case CABViewControlTypes.ORTS_POWER_LIMITATION_SELECTOR:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                    data = (ControlActiveLocomotive as MSTSElectricLocomotive)?.GetDataOf(cvc) ?? 0;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }
            return data;
        }
    }
}
