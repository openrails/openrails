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
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public abstract class ScriptedPowerSupply : IPowerSupply
    {
        protected PowerSupply AbstractScript;
        public TrainCar Car { get; protected set; }
        
        public MSTSWagon Wagon => Car as MSTSWagon;
        protected Simulator Simulator => Wagon.Simulator;
        protected Train Train => Wagon.Train;
        public Pantographs Pantographs => Wagon.Pantographs;
        protected int CarId = -1;

        public Battery Battery { get; protected set; }
        public BatterySwitch BatterySwitch => Battery.BatterySwitch;

        // Variables
        public List<TrainCar> ElectricTrainSupplySourceCars { get; set; } = new List<TrainCar>();
        public PowerSupplyState ElectricTrainSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public PowerSupplyDirection ElectricTrainSupplyDirection { get; set; } = PowerSupplyDirection.Sink;
        public bool ElectricTrainSupplyOn => ElectricTrainSupplyState == PowerSupplyState.PowerOn;
        public bool FrontElectricTrainSupplyCableConnected { get; set; }
        private float electricTrainSupplyPowerW = 0;
        public float ElectricTrainSupplyPowerW
        {

            get
            {
                if (ElectricTrainSupplyDirection == PowerSupplyDirection.Sink) return electricTrainSupplyPowerW;
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
            set
            {
                if (ElectricTrainSupplyDirection == PowerSupplyDirection.Sink) electricTrainSupplyPowerW = value;
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
        public ScriptedPowerSupply(MSTSWagon wagon)
        {
            Car = wagon;

            Battery = new Battery(Wagon);
        }
        public virtual void Copy(IPowerSupply other)
        {
            Battery.Copy(other.Battery);
        }
        public virtual void Initialize()
        {
            Battery.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public virtual void InitializeMoving()
        {
            Battery.InitializeMoving();

            AuxiliaryPowerSupplyState = PowerSupplyState.PowerOn;
            LowVoltagePowerSupplyState = PowerSupplyState.PowerOn;
            ElectricTrainSupplyState = PowerSupplyState.PowerOn;
            BatteryState = PowerSupplyState.PowerOn;

            AbstractScript?.InitializeMoving();
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
        }
        public virtual void Update(float elapsedClockSeconds)
        {
            int id = Train?.Cars.IndexOf(Car) ?? 0;
            if (id != CarId)
            {
                CarId = id;

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
            

            ElectricTrainSupplySourceCars.Clear();
            if (ElectricTrainSupplyDirection == PowerSupplyDirection.Sink && CarId >= 0)
            {
                // Check cars in front
                for (int i = CarId - 1; i >= 0; i--)
                {
                    var car = Train.Cars[i];
                    if (car.PowerSupply == null || !Train.Cars[i+1].PowerSupply.FrontElectricTrainSupplyCableConnected)
                    {
                        break;
                    }
                    if (car.PowerSupply.ElectricTrainSupplyDirection == PowerSupplyDirection.Source)
                    {
                        ElectricTrainSupplySourceCars.Add(car);
                    }
                }
                // Check cars in rear
                for (int i = CarId + 1; i < CarId; i++)
                {
                    var car = Train.Cars[i];
                    if (car.PowerSupply == null || !car.PowerSupply.FrontElectricTrainSupplyCableConnected)
                    {
                        break;
                    }
                    if (car.PowerSupply.ElectricTrainSupplyDirection == PowerSupplyDirection.Source)
                    {
                        ElectricTrainSupplySourceCars.Add(car);
                    }
                }

                ElectricTrainSupplyState = PowerSupplyState.PowerOff;
                foreach (var car in ElectricTrainSupplySourceCars)
                {
                    if (car.PowerSupply.ElectricTrainSupplyState > ElectricTrainSupplyState)
                    {
                        ElectricTrainSupplyState = car.PowerSupply.ElectricTrainSupplyState;
                    }
                }
            }
            else
            {
                electricTrainSupplyPowerW = 0;
            }

            Battery.Update(elapsedClockSeconds);
        }


        public void HandleEvent(PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEvent(evt, id);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEventFromLeadLocomotive(evt);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEventFromLeadLocomotive(evt, id);
        }

        protected virtual void AssignScriptFunctions()
        {
            // AbstractScriptClass
            AbstractScript.ClockTime = () => (float)Simulator.ClockTime;
            AbstractScript.GameTime = () => (float)Simulator.GameTime;
            AbstractScript.PreUpdate = () => Simulator.PreUpdate;
            AbstractScript.DistanceM = () => Wagon.DistanceM;
            AbstractScript.SpeedMpS = () => Math.Abs(Wagon.SpeedMpS);
            AbstractScript.Confirm = Simulator.Confirmer.Confirm;
            AbstractScript.Message = Simulator.Confirmer.Message;
            AbstractScript.SignalEvent = Wagon.SignalEvent;
            AbstractScript.SignalEventToTrain = (evt) => Train?.SignalEvent(evt);
        }

        public abstract void Parse(string lowercasetoken, STFReader stf);
    }
}
