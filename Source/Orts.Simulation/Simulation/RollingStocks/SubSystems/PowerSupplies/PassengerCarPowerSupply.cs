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
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class ScriptedPassengerCarPowerSupply : IPassengerCarPowerSupply, ISubSystem<ScriptedPassengerCarPowerSupply>
    {
        public TrainCar Car { get; }
        public MSTSWagon Wagon => Car as MSTSWagon;
        protected Simulator Simulator => Wagon.Simulator;
        protected Train Train => Wagon.Train;
        public Pantographs Pantographs => Wagon.Pantographs;
        protected int CarId = 0;

        public Battery Battery { get; protected set; }
        public BatterySwitch BatterySwitch => Battery.BatterySwitch;

        protected bool Activated = false;
        protected string ScriptName = "Default";
        protected PassengerCarPowerSupply Script;

        // Variables
        public List<MSTSLocomotive> ElectricTrainSupplyConnectedLocomotives = new List<MSTSLocomotive>();
        public PowerSupplyState ElectricTrainSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool ElectricTrainSupplyOn => ElectricTrainSupplyState == PowerSupplyState.PowerOn;
        public bool FrontElectricTrainSupplyCableConnected { get; set; }
        public float ElectricTrainSupplyPowerW { get; set; } = 0f;

        public PowerSupplyState LowVoltagePowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool LowVoltagePowerSupplyOn => LowVoltagePowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState BatteryState
        {
            get
            {
                return Battery.State;
            }
            set
            {
                Battery.State = value;
            }
        }
        public bool BatteryOn => BatteryState == PowerSupplyState.PowerOn;
        public float BatteryVoltageV => BatteryOn ? Battery.VoltageV : 0;

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

        private bool IsFirstUpdate = true;

        public ScriptedPassengerCarPowerSupply(MSTSWagon wagon)
        {
            Car = wagon;

            Battery = new Battery(Wagon);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
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

        public void Copy(IPowerSupply other)
        {
            if (other is ScriptedPassengerCarPowerSupply scriptedOther)
            {
                Copy(scriptedOther);
            }
        }

        public void Copy(ScriptedPassengerCarPowerSupply other)
        {
            Battery.Copy(other.Battery);

            ScriptName = other.ScriptName;

            PowerOnDelayS = other.PowerOnDelayS;
            ContinuousPowerW = other.ContinuousPowerW;
            HeatingPowerW = other.HeatingPowerW;
            AirConditioningPowerW = other.AirConditioningPowerW;
            AirConditioningYield = other.AirConditioningYield;
        }

        public virtual void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Wagon.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as PassengerCarPowerSupply;
                }
                if (Script == null)
                {
                    Script = new DefaultPassengerCarPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }

            Battery.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public virtual void InitializeMoving()
        {
            Battery.InitializeMoving();

            ElectricTrainSupplyState = PowerSupplyState.PowerOn;
            BatteryState = PowerSupplyState.PowerOn;

            Script?.InitializeMoving();
        }

        public virtual void Save(BinaryWriter outf)
        {
            Battery.Save(outf);

            outf.Write(FrontElectricTrainSupplyCableConnected);

            outf.Write(ElectricTrainSupplyState.ToString());
            outf.Write(LowVoltagePowerSupplyState.ToString());
            outf.Write(BatteryState.ToString());
            outf.Write(VentilationState.ToString());
            outf.Write(HeatingState.ToString());
            outf.Write(AirConditioningState.ToString());

            outf.Write(HeatFlowRateW);
        }

        public virtual void Restore(BinaryReader inf)
        {
            Battery.Restore(inf);

            FrontElectricTrainSupplyCableConnected = inf.ReadBoolean();

            ElectricTrainSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            LowVoltagePowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            BatteryState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            VentilationState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            HeatingState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            AirConditioningState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            HeatFlowRateW = inf.ReadSingle();

            IsFirstUpdate = false;
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            CarId = Train?.Cars.IndexOf(Wagon) ?? 0;

            if (IsFirstUpdate)
            {
                IsFirstUpdate = false;

                // At this point, we can expect Train to be initialized.
                var previousCar = CarId > 0 ? Train.Cars[CarId - 1] : null;

                // Connect the power supply cable if the previous car is a locomotive or another passenger car
                if (previousCar != null
                    && (previousCar is MSTSLocomotive locomotive && locomotive.LocomotivePowerSupply.ElectricTrainSupplyState != PowerSupplyState.Unavailable
                        || previousCar.WagonSpecialType == TrainCar.WagonSpecialTypes.PowerVan
                        || previousCar.WagonType == TrainCar.WagonTypes.Passenger && previousCar.PowerSupply is ScriptedPassengerCarPowerSupply)
                    )
                {
                    FrontElectricTrainSupplyCableConnected = true;
                }
            }

            ElectricTrainSupplyConnectedLocomotives.Clear();
            foreach (TrainCar car in Train.Cars)
            {
                if (car is MSTSLocomotive locomotive)
                {
                    int locomotiveId = Train.Cars.IndexOf(locomotive);
                    bool locomotiveInFront = locomotiveId < CarId;

                    bool connectedToLocomotive = true;
                    if (locomotiveInFront)
                    {
                        for (int i = locomotiveId; i < CarId; i++)
                        {
                            if (Train.Cars[i + 1].PowerSupply == null)
                            {
                                connectedToLocomotive = false;
                                break;
                            }
                            if (!Train.Cars[i + 1].PowerSupply.FrontElectricTrainSupplyCableConnected)
                            {
                                connectedToLocomotive = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = locomotiveId; i > CarId; i--)
                        {
                            if (Train.Cars[i].PowerSupply == null)
                            {
                                connectedToLocomotive = false;
                                break;
                            }
                            if (!Train.Cars[i].PowerSupply.FrontElectricTrainSupplyCableConnected)
                            {
                                connectedToLocomotive = false;
                                break;
                            }
                        }
                    }
                    
                    if (connectedToLocomotive) ElectricTrainSupplyConnectedLocomotives.Add(locomotive);
                }
            }

            ElectricTrainSupplyState = PowerSupplyState.PowerOff;
            foreach (var locomotive in ElectricTrainSupplyConnectedLocomotives)
            {
                if (locomotive.LocomotivePowerSupply.ElectricTrainSupplyState > ElectricTrainSupplyState)
                {
                    ElectricTrainSupplyState = locomotive.LocomotivePowerSupply.ElectricTrainSupplyState;
                }
            }

            Battery.Update(elapsedClockSeconds);
            Script?.Update(elapsedClockSeconds);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Script?.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            Script?.HandleEvent(evt, id);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            Script?.HandleEventFromLeadLocomotive(evt);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
            Script?.HandleEventFromLeadLocomotive(evt, id);
        }

        protected virtual void AssignScriptFunctions()
        {
            // AbstractScriptClass
            Script.ClockTime = () => (float)Simulator.ClockTime;
            Script.GameTime = () => (float)Simulator.GameTime;
            Script.PreUpdate = () => Simulator.PreUpdate;
            Script.DistanceM = () => Wagon.DistanceM;
            Script.SpeedMpS = () => Math.Abs(Wagon.SpeedMpS);
            Script.Confirm = Simulator.Confirmer.Confirm;
            Script.Message = Simulator.Confirmer.Message;
            Script.SignalEvent = Wagon.SignalEvent;
            Script.SignalEventToTrain = (evt) => Train?.SignalEvent(evt);
        }
    }

    public class DefaultPassengerCarPowerSupply : PassengerCarPowerSupply
    {
        private Timer PowerOnTimer;
        PowerSupplyState PassengerPowerSupplyState;

        public override void Initialize()
        {
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            PassengerPowerSupplyState = PowerSupplyState.PowerOff;
            SetCurrentVentilationState(PowerSupplyState.PowerOff);
            SetCurrentHeatingState(PowerSupplyState.PowerOff);
            SetCurrentAirConditioningState(PowerSupplyState.PowerOff);
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
                    if (PassengerPowerSupplyState != PowerSupplyState.PowerOff)
                    {
                        PassengerPowerSupplyState = PowerSupplyState.PowerOff;
                        SignalEvent(Event.PowerConverterOff);
                    }
                    break;

                case PowerSupplyState.PowerOn:
                    if (!PowerOnTimer.Started)
                        PowerOnTimer.Start();
                    switch (PassengerPowerSupplyState)
                    {
                        case PowerSupplyState.PowerOff:
                            PassengerPowerSupplyState = PowerSupplyState.PowerOnOngoing;
                            break;
                        case PowerSupplyState.PowerOnOngoing:
                            if (PowerOnTimer.Triggered)
                            {
                                PassengerPowerSupplyState = PowerSupplyState.PowerOn;
                                SignalEvent(Event.PowerConverterOn);
                            }
                            break;
                    }
                    break;
            }

            switch (PassengerPowerSupplyState)
            {
                case PowerSupplyState.PowerOff:
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

                    SetCurrentElectricTrainSupplyPowerW(ContinuousPowerW() + heatingPowerW + airConditioningElectricPowerW);
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
