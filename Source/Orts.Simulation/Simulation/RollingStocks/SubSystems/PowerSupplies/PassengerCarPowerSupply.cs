// COPYRIGHT 2021 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class ScriptedPassengerCarPowerSupply : ScriptedPowerSupply
    {
        protected bool Activated = false;
        protected string ScriptName = "Default";
        protected PassengerCarPowerSupply Script => AbstractScript as PassengerCarPowerSupply;

        public PowerSupplyState VentilationState { get; set; }
        public PowerSupplyState HeatingState { get; set; }
        public PowerSupplyState AirConditioningState { get; set; }
        public float HeatFlowRateW { get; set; }

        // Parameters
        public float PowerOnDelayS { get; protected set; } = 0f;
        public float ContinuousPowerW { get; protected set; } = 0f;
        public float HeatingPowerW { get; protected set; } = 0f;
        public float AirConditioningPowerW { get; protected set; } = 0f;
        public float AirConditioningYield { get; protected set; } = 0.9f;

        public ScriptedPassengerCarPowerSupply(MSTSWagon wagon) : base(wagon)
        {
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "wagon(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "wagon(ortsbattery":
                    Battery.Parse(lowercasetoken, stf);
                    break;

                case "wagon(ortspowersupplycontinuouspower":
                    ContinuousPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0f);
                    break;

                case "wagon(ortspowersupplyheatingpower":
                    HeatingPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0f);
                    break;

                case "wagon(ortspowersupplyairconditioningpower":
                    AirConditioningPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0f);
                    break;

                case "wagon(ortspowersupplyairconditioningyield":
                    AirConditioningYield = stf.ReadFloatBlock(STFReader.UNITS.Power, 0.9f);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);
            if (other is ScriptedPassengerCarPowerSupply scriptedOther)
            {
                ScriptName = scriptedOther.ScriptName;

                PowerOnDelayS = scriptedOther.PowerOnDelayS;
                ContinuousPowerW = scriptedOther.ContinuousPowerW;
                HeatingPowerW = scriptedOther.HeatingPowerW;
                AirConditioningPowerW = scriptedOther.AirConditioningPowerW;
                AirConditioningYield = scriptedOther.AirConditioningYield;
            }
        }

        public override void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Wagon.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as PassengerCarPowerSupply;
                }
                if (Script == null)
                {
                    AbstractScript = new DefaultPassengerCarPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }
            base.Initialize();
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);

            outf.Write(VentilationState.ToString());
            outf.Write(HeatingState.ToString());
            outf.Write(AirConditioningState.ToString());

            outf.Write(HeatFlowRateW);
        }

        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);

            VentilationState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            HeatingState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            AirConditioningState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            HeatFlowRateW = inf.ReadSingle();
        }
    }

    public class DefaultPassengerCarPowerSupply : PassengerCarPowerSupply
    {
        private Timer PowerOnTimer;
        PowerSupplyLoad ContinuousLoads = new PowerSupplyLoad(PowerSupplyLoadType.Other);
        PowerSupplyLoad AirConditioning = new PowerSupplyLoad(PowerSupplyLoadType.AirConditioning);
        PowerSupplyLoad Heating = new PowerSupplyLoad(PowerSupplyLoadType.CarHeating);

        public override void Initialize()
        {
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            SetCurrentVentilationState(PowerSupplyState.PowerOff);
            SetCurrentHeatingState(PowerSupplyState.PowerOff);
            SetCurrentAirConditioningState(PowerSupplyState.PowerOff);

            AuxiliaryPowerSupplyLoads.Add(ContinuousLoads);
            AuxiliaryPowerSupplyLoads.Add(AirConditioning);
            AuxiliaryPowerSupplyLoads.Add(Heating);
        }

        public override void InitializeMoving()
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() || CurrentElectricTrainSupplyState() == PowerSupplyState.PowerOn ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentElectricTrainSupplyState())
            {
                case PowerSupplyState.PowerOff:
                    if (PowerOnTimer.Started)
                        PowerOnTimer.Stop();
                    if (CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOff)
                    {
                        SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.PowerConverterOff);
                    }
                    break;

                case PowerSupplyState.PowerOn:
                    if (!PowerOnTimer.Started)
                        PowerOnTimer.Start();
                    switch (CurrentAuxiliaryPowerSupplyState())
                    {
                        case PowerSupplyState.PowerOff:
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOnOngoing);
                            SignalEvent(Event.PowerConverterOn);
                            break;
                        case PowerSupplyState.PowerOnOngoing:
                            if (PowerOnTimer.Triggered)
                            {
                                SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                            }
                            break;
                    }
                    break;
            }

            switch (CurrentAuxiliaryPowerSupplyState())
            {
                default:
                    if (CurrentVentilationState() == PowerSupplyState.PowerOn)
                    {
                        SetCurrentVentilationState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.VentilationOff);
                    }

                    if (CurrentHeatingState() == PowerSupplyState.PowerOn)
                    {
                        SetCurrentHeatingState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.HeatingOff);
                    }

                    if (CurrentAirConditioningState() == PowerSupplyState.PowerOn)
                    {
                        SetCurrentAirConditioningState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.AirConditioningOff);
                    }

                    AirConditioning.CurrentPowerW = 0;
                    Heating.CurrentPowerW = 0;
                    ContinuousLoads.CurrentPowerW = 0;

                    SetCurrentElectricTrainSupplyPowerW(0f);
                    SetCurrentHeatFlowRateW(0f);
                    break;
                case PowerSupplyState.PowerOn:
                    if (CurrentVentilationState() == PowerSupplyState.PowerOff)
                    {
                        SetCurrentVentilationState(PowerSupplyState.PowerOn);
                        SignalEvent(Event.VentilationLow);
                    }

                    if (CurrentHeatingState() == PowerSupplyState.PowerOff
                        && InsideTemperatureC() < DesiredTemperatureC() - 2.5f)
                    {
                        SetCurrentHeatingState(PowerSupplyState.PowerOn);
                        SignalEvent(Event.HeatingOn);
                    }
                    else if (CurrentHeatingState() == PowerSupplyState.PowerOn
                        && InsideTemperatureC() >= DesiredTemperatureC())
                    {
                        SetCurrentHeatingState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.HeatingOff);
                    }

                    float heatingPowerW = CurrentHeatingState() == PowerSupplyState.PowerOn ? HeatingPowerW() : 0f;

                    if (CurrentAirConditioningState() == PowerSupplyState.PowerOff
                        && InsideTemperatureC() > DesiredTemperatureC() + 2.5f)
                    {
                        SetCurrentAirConditioningState(PowerSupplyState.PowerOn);
                        SignalEvent(Event.AirConditioningOn);
                    }
                    else if (CurrentAirConditioningState() == PowerSupplyState.PowerOn
                        && InsideTemperatureC() <= DesiredTemperatureC())
                    {
                        SetCurrentAirConditioningState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.AirConditioningOff);
                    }

                    float airConditioningElectricPowerW = CurrentAirConditioningState() == PowerSupplyState.PowerOn ? AirConditioningPowerW() : 0f;
                    float airConditioningThermalPowerW = CurrentAirConditioningState() == PowerSupplyState.PowerOn ? - AirConditioningPowerW() * AirConditioningYield() : 0f;

                    AirConditioning.CurrentPowerW = airConditioningElectricPowerW;
                    Heating.CurrentPowerW = heatingPowerW;
                    ContinuousLoads.CurrentPowerW = ContinuousPowerW();

                    SetCurrentElectricTrainSupplyPowerW(AuxiliaryPowerSupplyPowerW);
                    SetCurrentHeatFlowRateW(heatingPowerW + airConditioningThermalPowerW);
                    break;
            }
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            SignalEventToPantographs(evt);
            SignalEventToBatterySwitch(evt);
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            SignalEventToPantograph(evt, id);
        }
    }

}
