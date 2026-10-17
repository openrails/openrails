// COPYRIGHT 2026 by the Open Rails project.
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
    public class ScriptedPowerVanPowerSupply : IPowerSupply
    {
        public TrainCar Car { get; }
        public MSTSWagon Wagon => Car as MSTSWagon;
        protected Simulator Simulator => Wagon.Simulator;
        protected Train Train => Wagon.Train;
        public Pantographs Pantographs => Wagon.Pantographs;
        public DieselEngines DieselEngines = null;
        protected int CarId = 0;

        public Battery Battery { get; protected set; }
        public BatterySwitch BatterySwitch => Battery.BatterySwitch;

        protected bool Activated = false;
        protected string ScriptName = "Default";
        protected PowerVanPowerSupply Script;

        // Variables
        public List<TrainCar> ElectricTrainSupplySourceCars { get; set; } = new List<TrainCar>();
        public PowerSupplyState ElectricTrainSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public PowerSupplyDirection ElectricTrainSupplyDirection { get; set; } = PowerSupplyDirection.Source;
        public bool ElectricTrainSupplyOn => ElectricTrainSupplyState == PowerSupplyState.PowerOn;
        public bool FrontElectricTrainSupplyCableConnected { get; set; }
        public float ElectricTrainSupplyPowerW
        {
            get
            {
                if (!ElectricTrainSupplyOn) return 0;
                float result = 0;
                foreach (var car in Train.Cars)
                {
                    if (!(car is MSTSWagon wagon) || wagon.PowerSupply == null) continue;
                    if (wagon.PowerSupply.ElectricTrainSupplyDirection == PowerSupplyDirection.Sink)
                    {
                        bool found = false;
                        int count = 0;
                        foreach (var car2 in wagon.PowerSupply.ElectricTrainSupplySourceCars)
                        {
                            if (car2 == Car) found = true;
                            if (car2.PowerSupply?.ElectricTrainSupplyOn ?? false) ++count;
                        }
                        if (found) result += wagon.PowerSupply.ElectricTrainSupplyPowerW / count;
                    }
                }
                return result;
            }
        }

        public PowerSupplyState AuxiliaryPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool AuxiliaryPowerSupplyOn => AuxiliaryPowerSupplyState == PowerSupplyState.PowerOn;
        public float AuxiliaryPowerSupplyPowerW
        {
            get
            {
                if (!AuxiliaryPowerSupplyOn) return 0;
                float powerW = 0;
                foreach (var load in AuxiliaryPowerSupplyLoads)
                {
                    powerW += load.CurrentPowerW;
                }
                return powerW;
            }
        }
        public List<PowerSupplyLoad> AuxiliaryPowerSupplyLoads { get; protected set; } = new List<PowerSupplyLoad>();

        public PowerSupplyState LowVoltagePowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool LowVoltagePowerSupplyOn => LowVoltagePowerSupplyState == PowerSupplyState.PowerOn;
        public float LowVoltagePowerSupplyPowerW
        {
            get
            {
                if (!LowVoltagePowerSupplyOn) return 0;
                float powerW = 0;
                foreach (var load in LowVoltagePowerSupplyLoads)
                {
                    powerW += load.CurrentPowerW;
                }
                return powerW;
            }
        }
        public List<PowerSupplyLoad> LowVoltagePowerSupplyLoads { get; protected set; } = new List<PowerSupplyLoad>();

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
        public float PowerOnDelayS { get; protected set; } = 0f;

        private bool IsFirstUpdate = true;

        public ScriptedPowerVanPowerSupply(MSTSWagon wagon)
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
            }
        }

        public void Copy(IPowerSupply other)
        {
            if (other is ScriptedPowerVanPowerSupply scriptedOther)
            {
                Copy(scriptedOther);
            }
        }

        public void Copy(ScriptedPowerVanPowerSupply other)
        {
            Battery.Copy(other.Battery);

            ScriptName = other.ScriptName;

            PowerOnDelayS = other.PowerOnDelayS;
        }

        public virtual void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Wagon.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as PowerVanPowerSupply;
                }
                if (Script == null)
                {
                    Script = new DefaultPowerVanPowerSupply();
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
        }

        public virtual void Restore(BinaryReader inf)
        {
            Battery.Restore(inf);

            FrontElectricTrainSupplyCableConnected = inf.ReadBoolean();

            ElectricTrainSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            LowVoltagePowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            BatteryState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

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
    public class DefaultPowerVanPowerSupply : PowerVanPowerSupply
    {
        private Timer PowerOnTimer;

        private bool QuickPowerOn = false;

        public override void Initialize()
        {
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());
        }

        public override void InitializeMoving()
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() || CurrentElectricTrainSupplyState() == PowerSupplyState.PowerOn ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            if (QuickPowerOn)
            {
                if (CurrentPantographState() == PantographState.Up)
                {
                    SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOn);
                    QuickPowerOn = false;
                }
                if (CurrentDieselEnginesState() == DieselEngineState.Running)
                {
                    QuickPowerOn = false;
                }
            }

            if ((CurrentPantographState() == PantographState.Up && CurrentCircuitBreakerState() == CircuitBreakerState.Closed)
                || CurrentDieselEnginesState() == DieselEngineState.Running)
            {
                if (!PowerOnTimer.Started)
                {
                    PowerOnTimer.Start();
                    SignalEvent(Event.PowerConverterOn);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOnOngoing);
                }

                if (PowerOnTimer.Triggered && CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOn)
                {
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                }
            }
            else
            {
                if (PowerOnTimer.Started)
                    PowerOnTimer.Stop();
                if (CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOff)
                {
                    SignalEvent(Event.PowerConverterOff);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    PowerOnTimer.Stop();
                }
            }
            SetCurrentElectricTrainSupplyState(CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
        }
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToDieselEngines(PowerSupplyEvent.StartEngine);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOff);
                    SignalEventToPantographs(PowerSupplyEvent.LowerPantograph);
                    SignalEventToDieselEngines(PowerSupplyEvent.StopEngine);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
