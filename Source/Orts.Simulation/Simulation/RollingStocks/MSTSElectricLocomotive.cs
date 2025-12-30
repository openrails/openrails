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

using System;
using System.IO;
using System.Text;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph control to the basic LocomotiveSimulator functionality
    /// </summary>
    public class MSTSElectricLocomotive : MSTSLocomotive
    {
        public ScriptedElectricPowerSupply ElectricPowerSupply => PowerSupply as ScriptedElectricPowerSupply;

        public MSTSElectricLocomotive(Simulator simulator, string wagFile) :
            base(simulator, wagFile)
        {
            PowerSupply = new ScriptedElectricPowerSupply(this);
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
                case "engine(ortspowersupplyparameters":
                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                case "engine(ortsbattery":
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                case "engine(ortsvoltageselector":
                case "engine(ortspantographselector":
                case "engine(ortspowerlimitationselector":
                case "engine(ortselectrictrainsupply(mode":
                    LocomotivePowerSupply.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(CurrentLocomotiveSteamHeatBoilerWaterCapacityL);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            CurrentLocomotiveSteamHeatBoilerWaterCapacityL = inf.ReadSingle();
            base.Restore(inf);
        }

        public override void Initialize(bool reinitialize = false)
        {
            base.Initialize(reinitialize);

            // If DrvWheelWeight is not in ENG file, then calculate drivewheel weight freom FoA

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                DrvWheelWeightKg = MassKG; // set Drive wheel weight to total wagon mass if not in ENG file
            }

            // Initialise water level in steam heat boiler
            if (CurrentLocomotiveSteamHeatBoilerWaterCapacityL == 0 && IsSteamHeatFitted)
            {
                if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = MaximumSteamHeatBoilerWaterTankCapacityL;
                }
                else
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = L.FromGUK(800.0f);
                }
            }

            if (Simulator.Settings.ElectricHotStart) SetPower(true);
        }

        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        /// 
        public override void InitializeMoving()
        {
            base.InitializeMoving();
            WheelSpeedMpS = SpeedMpS;
            DynamicBrakePercent = -1;
            ThrottleController.SetValue(Train.MUThrottlePercent / 100);

            Pantographs.InitializeMoving();
        }

        /// <summary>
        /// This function updates periodically the wagon heating.
        /// </summary>
        protected override void UpdateCarSteamHeat(float elapsedClockSeconds)
        {
            // Update Steam Heating System

            // TO DO - Add test to see if cars are coupled, if Light Engine, disable steam heating.


            if (IsSteamHeatFitted && this.IsLeadLocomotive())  // Only Update steam heating if train and locomotive fitted with steam heating
            {

                // Update water controller for steam boiler heating tank
                    WaterController.Update(elapsedClockSeconds);
                    if (WaterController.UpdateValue > 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeatBoilerWater, CabSetting.Increase, WaterController.CurrentValue * 100);


                CurrentSteamHeatPressurePSI = SteamHeatController.CurrentValue * MaxSteamHeatPressurePSI;

                // Calculate steam boiler usage values
                // Don't turn steam heat on until pressure valve has been opened, water and fuel capacity also needs to be present, and steam boiler is not locked out
                if (CurrentSteamHeatPressurePSI > 0.1 && CurrentLocomotiveSteamHeatBoilerWaterCapacityL > 0 && CurrentSteamHeatBoilerFuelCapacityL > 0 && !IsSteamHeatBoilerLockedOut)
                {
                    // Set values for visible exhaust based upon setting of steam controller
                    HeatingSteamBoilerVolumeM3pS = 1.5f * SteamHeatController.CurrentValue;
                    HeatingSteamBoilerDurationS = 1.0f * SteamHeatController.CurrentValue;
                    Train.CarSteamHeatOn = true; // turn on steam effects on wagons

                    // Calculate fuel usage for steam heat boiler
                    float FuelUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerFuelUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    CurrentSteamHeatBoilerFuelCapacityL -= FuelUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as fuel used.

                    // Calculate water usage for steam heat boiler
                    float WaterUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerWaterUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL -= WaterUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as water used. Weight of locomotive is reduced in Wagon.cs
                }
                else
                {
                    Train.CarSteamHeatOn = false; // turn on steam effects on wagons
                }


            }
        }


        /// <summary>
        /// This function updates periodically the locomotive's sound variables.
        /// </summary>
        protected override void UpdateSoundVariables(float elapsedClockSeconds)
        {
            Variable1 = ThrottlePercent;
            if (ThrottlePercent == 0f) Variable2 = 0;
            else
            {
                float dV2;
                dV2 = Math.Abs(TractiveForceN) / MaxForceN * 100f - Variable2;
                float max = 2f;
                if (dV2 > max) dV2 = max;
                else if (dV2 < -max) dV2 = -max;
                Variable2 += dV2;
            }
            if (DynamicBrakePercent > 0)
                Variable3 = MaxDynamicBrakeForceN == 0 ? DynamicBrakePercent / 100f : DynamicBrakeForceN / MaxDynamicBrakeForceN;
            else
                Variable3 = 0;
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType.Type)
            {
                case CABViewControlTypes.LINE_VOLTAGE:
                    data = ElectricPowerSupply.PantographVoltageV;
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH_VOLTAGE_AC:
                    data = ElectricPowerSupply.PantographVoltageVAC;
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH_VOLTAGE_DC:
                    data = ElectricPowerSupply.PantographVoltageVDC;
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;
                
                case CABViewControlTypes.PANTO_DISPLAY:
                    data = Pantographs.State == PantographState.Up ? 1 : 0;
                    break;

                case CABViewControlTypes.PANTOGRAPH:
                    data = Pantographs[1].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.PANTOGRAPH2:
                    data = Pantographs[2].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH3:
                    data = Pantographs.List.Count > 2 && Pantographs[3].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH4:
                    data = Pantographs.List.Count > 3 && Pantographs[4].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.PANTOGRAPHS_4:
                case CABViewControlTypes.PANTOGRAPHS_4C:
                    if (Pantographs[1].CommandUp && Pantographs[2].CommandUp)
                        data = 2;
                    else if (Pantographs[1].CommandUp)
                        data = 1;
                    else if (Pantographs[2].CommandUp)
                        data = 3;
                    else
                        data = 0;
                    break;

                case CABViewControlTypes.PANTOGRAPHS_5:
                    if (Pantographs[1].CommandUp && Pantographs[2].CommandUp)
                        data = 0; // TODO: Should be 0 if the previous state was Pan2Up, and 4 if that was Pan1Up
                    else if (Pantographs[2].CommandUp)
                        data = 1;
                    else if (Pantographs[1].CommandUp)
                        data = 3;
                    else
                        data = 2;
                    break;

                case CABViewControlTypes.ORTS_VOLTAGE_SELECTOR:
                    data = ElectricPowerSupply.VoltageSelector.PositionId;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH_SELECTOR:
                    data = ElectricPowerSupply.PantographSelector.PositionId;
                    break;

                case CABViewControlTypes.ORTS_POWER_LIMITATION_SELECTOR:
                    data = ElectricPowerSupply.PowerLimitationSelector.PositionId;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                    data = ElectricPowerSupply.CircuitBreaker.DriverClosingOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                    data = ElectricPowerSupply.CircuitBreaker.DriverOpeningOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                    data = ElectricPowerSupply.CircuitBreaker.DriverClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                    switch (ElectricPowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                            data = 0;
                            break;
                        case CircuitBreakerState.Closing:
                            data = 1;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 2;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                    switch (ElectricPowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                        case CircuitBreakerState.Closing:
                            data = 0;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 1;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                    switch (ElectricPowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                        case CircuitBreakerState.Closing:
                            data = 1;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 0;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                    data = ElectricPowerSupply.CircuitBreaker.ClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                    data = (ElectricPowerSupply.CircuitBreaker.State < CircuitBreakerState.Closed && ElectricPowerSupply.CircuitBreaker.ClosingAuthorization) ? 1 : 0;
                    break;

                default:
                    data = base.GetDataOf(cvc);
                    break;
            }

            return data;
        }

        public override void SwitchToAutopilotControl()
        {
            SetDirection(Direction.Forward);
            base.SwitchToAutopilotControl();
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("{0} = ", Simulator.Catalog.GetString("Pantographs"));
            foreach (var pantograph in Pantographs.List)
                status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(pantograph.State)));
            status.AppendLine();
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Battery switch"),
                LocomotivePowerSupply.BatterySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Master key"),
                LocomotivePowerSupply.MasterKey.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Circuit breaker"),
                Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(ElectricPowerSupply.CircuitBreaker.State)));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Electric train supply"),
                LocomotivePowerSupply.ElectricTrainSupplySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendLine();
            status.AppendFormat("{0} = {1}",
                Simulator.Catalog.GetParticularString("PowerSupply", "Power"),
                Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(LocomotivePowerSupply.MainPowerSupplyState)));
            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());
            status.AppendFormat("\t{0}\t\t{1}", Simulator.Catalog.GetString("Circuit breaker"), Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(ElectricPowerSupply.CircuitBreaker.State)));
            status.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetString("TCS"), ElectricPowerSupply.CircuitBreaker.TCSClosingAuthorization ? Simulator.Catalog.GetString("OK") : Simulator.Catalog.GetString("NOT OK"));
            status.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetString("Driver"), ElectricPowerSupply.CircuitBreaker.DriverClosingAuthorization ? Simulator.Catalog.GetString("OK") : Simulator.Catalog.GetString("NOT OK"));
            status.AppendFormat("\t{0}\t\t{1}\n", Simulator.Catalog.GetString("Auxiliary power"), Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(LocomotivePowerSupply.AuxiliaryPowerSupplyState)));

            if (IsSteamHeatFitted && Train.PassengerCarsNumber > 0 && this.IsLeadLocomotive() && Train.CarSteamHeatOn)
            {
                // Only show steam heating HUD if fitted to locomotive and the train, has passenger cars attached, and is the lead locomotive
                // Display Steam Heat info
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}/{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18:N0}\n",
                   Simulator.Catalog.GetString("StHeat:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(CurrentSteamHeatPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("StTemp"),
                   FormatStrings.FormatTemperature(C.FromF(SaturatedSteamHeatPressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]), IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
                   Simulator.Catalog.GetString("WaterLvl"),
                   FormatStrings.FormatFuelVolume(CurrentLocomotiveSteamHeatBoilerWaterCapacityL, IsMetric, IsUK),
                   Simulator.Catalog.GetString("Last:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(Train.LastCar.CarSteamHeatMainPipeSteamPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("Temp"),
                   FormatStrings.FormatTemperature(Train.LastCar.CarInsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("OutTemp"),
                   FormatStrings.FormatTemperature(CarOutsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("NetHt"),
                   Train.LastCar.CarNetHeatFlowRateW);
            }

            return status.ToString();
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            MSTSNotchController controller = null;
            if (type == (uint)PickupType.FuelWater) return WaterController;
            return controller;
        }

        /// <summary>
        /// Sets step size for the fuel controller basing on pickup feed rate and engine fuel capacity
        /// </summary>
        /// <param name="type">Pickup</param>

        public override void SetStepSize(PickupObj matchPickup)
        {
            if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                WaterController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / MaximumSteamHeatBoilerWaterTankCapacityL);
        }

        /// <summary>
        /// Sets coal and water supplies to full immediately.
        /// Provided in case route lacks pickup points for diesel oil.
        /// </summary>
        public override void RefillImmediately()
        {
            WaterController.CurrentValue = 1.0f;
        }

        /// <summary>
        /// Returns the fraction of diesel oil already in tank.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            if (pickupType == (uint)PickupType.FuelWater)
            {
                return WaterController.CurrentValue;
            }
            return 0f;
        }

    } // class ElectricLocomotive
}
