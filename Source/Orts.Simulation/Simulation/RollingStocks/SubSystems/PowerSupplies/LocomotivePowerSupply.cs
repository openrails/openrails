// COPYRIGHT 2020 by the Open Rails project.
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

    public abstract class ScriptedLocomotivePowerSupply : ILocomotivePowerSupply
    {
        public TrainCar Car { get; protected set; }
        public MSTSLocomotive Locomotive => Car as MSTSLocomotive;
        protected Simulator Simulator => Locomotive.Simulator;
        protected Train Train => Locomotive.Train;
        protected int CarId = 0;

        public Battery Battery {get; protected set; }
        public BatterySwitch BatterySwitch => Battery.BatterySwitch;
        public Pantographs Pantographs => Locomotive.Pantographs;
        public MasterKey MasterKey { get; protected set; }
        public ElectricTrainSupplySwitch ElectricTrainSupplySwitch { get; protected set; }

        public abstract PowerSupplyType Type { get; }
        protected string ScriptName = "Default";
        public string ParametersFileName { get; protected set; }
        protected LocomotivePowerSupply AbstractScript;

        public PowerSupplyState MainPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool MainPowerSupplyOn => MainPowerSupplyState == PowerSupplyState.PowerOn;
        public float MaximumPowerW;
        public float AvailableTractionPowerW = float.MaxValue;
        public bool DynamicBrakeAvailable { get; set; } = false;
        public float PowerSupplyDynamicBrakePercent { get; set; } = -1;
        public float MaximumDynamicBrakePowerW { get; set; } = 0;
        public float MaxThrottlePercent { get; set; } = 100;
        public float ThrottleReductionPercent { get; set; } = 0;

        public PowerSupplyState AuxiliaryPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool AuxiliaryPowerSupplyOn => AuxiliaryPowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState ElectricTrainSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool ElectricTrainSupplyOn => ElectricTrainSupplyState == PowerSupplyState.PowerOn;
        public bool FrontElectricTrainSupplyCableConnected { get; set; }
        public float ElectricTrainSupplyPowerW
        {
            get
            {
                float result = 0;
                foreach (var car in Train.Cars)
                {
                    if (car == null) continue;
                    if (!(car is MSTSWagon wagon)) continue;
                    if (!(wagon.PassengerCarPowerSupply?.ElectricTrainSupplyConnectedLocomotives.Contains(Locomotive) ?? false)) continue;
                    result += wagon.PassengerCarPowerSupply.ElectricTrainSupplyPowerW / wagon.PassengerCarPowerSupply.ElectricTrainSupplyConnectedLocomotives.Count;
                }
                return result;
            }
        }

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

        public PowerSupplyState CabPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool CabPowerSupplyOn => CabPowerSupplyState == PowerSupplyState.PowerOn;

        public float PowerOnDelayS { get; protected set; } = 0f;
        public float AuxPowerOnDelayS { get; protected set; } = 0f;

        public bool ServiceRetentionButton { get; set; } = false;
        public bool ServiceRetentionCancellationButton { get; set; } = false;
        public bool ServiceRetentionActive { get; set; } = false;
        public Dictionary<int, float> CabDisplayControls = new Dictionary<int, float>();

        // generic power supply commands
        public Dictionary<int, bool> PowerSupplyCommandButtonDown = new Dictionary<int, bool>();
        public Dictionary<int, bool> PowerSupplyCommandSwitchOn = new Dictionary<int, bool>();
        // List of customized control strings;
        public Dictionary<int, string> CustomizedCabviewControlNames = new Dictionary<int, string>();

        private bool firstUpdate = true;

        protected ScriptedLocomotivePowerSupply(MSTSLocomotive locomotive)
        {
            Car = locomotive;

            Battery = new Battery(Locomotive);
            MasterKey = new MasterKey(Locomotive);
            ElectricTrainSupplySwitch = new ElectricTrainSupplySwitch(Locomotive);

            MainPowerSupplyState = PowerSupplyState.PowerOff;
            AuxiliaryPowerSupplyState = PowerSupplyState.PowerOff;
            LowVoltagePowerSupplyState = PowerSupplyState.PowerOff;
            CabPowerSupplyState = PowerSupplyState.PowerOff;
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortspowersupplyparameters":
                    ParametersFileName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsauxpowerondelay":
                    AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsbattery":
                    Battery.Parse(lowercasetoken, stf);
                    break;
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                    MasterKey.Parse(lowercasetoken, stf);
                    break;

                case "engine(ortselectrictrainsupply(mode":
                    ElectricTrainSupplySwitch.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public virtual void Copy(IPowerSupply other)
        {
            if (other is ScriptedLocomotivePowerSupply scriptedOther)
            {
                Battery.Copy(scriptedOther.Battery);
                MasterKey.Copy(scriptedOther.MasterKey);
                ElectricTrainSupplySwitch.Copy(scriptedOther.ElectricTrainSupplySwitch);

                ScriptName = scriptedOther.ScriptName;
                ParametersFileName = scriptedOther.ParametersFileName;

                PowerOnDelayS = scriptedOther.PowerOnDelayS;
                AuxPowerOnDelayS = scriptedOther.AuxPowerOnDelayS;
            }
        }

        public virtual void Initialize()
        {
            Battery.Initialize();
            MasterKey.Initialize();
            ElectricTrainSupplySwitch.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public virtual void InitializeMoving()
        {
            Battery.InitializeMoving();
            MasterKey.InitializeMoving();
            ElectricTrainSupplySwitch.InitializeMoving();

            MainPowerSupplyState = PowerSupplyState.PowerOn;
            AuxiliaryPowerSupplyState = PowerSupplyState.PowerOn;
            ElectricTrainSupplyState = PowerSupplyState.PowerOn;
            LowVoltagePowerSupplyState = PowerSupplyState.PowerOn;
            BatteryState = PowerSupplyState.PowerOn;
            if (Locomotive.IsLeadLocomotive())
            {
                CabPowerSupplyState = PowerSupplyState.PowerOn;
            }

            AbstractScript?.InitializeMoving();
        }

        public virtual void Save(BinaryWriter outf)
        {
            Battery.Save(outf);
            MasterKey.Save(outf);
            ElectricTrainSupplySwitch.Save(outf);

            outf.Write(FrontElectricTrainSupplyCableConnected);

            outf.Write(MainPowerSupplyState.ToString());
            outf.Write(AuxiliaryPowerSupplyState.ToString());
            outf.Write(ElectricTrainSupplyState.ToString());
            outf.Write(LowVoltagePowerSupplyState.ToString());
            outf.Write(BatteryState.ToString());
            outf.Write(CabPowerSupplyState.ToString());
        }

        public virtual void Restore(BinaryReader inf)
        {
            Battery.Restore(inf);
            MasterKey.Restore(inf);
            ElectricTrainSupplySwitch.Restore(inf);

            FrontElectricTrainSupplyCableConnected = inf.ReadBoolean();

            MainPowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            AuxiliaryPowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            ElectricTrainSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            LowVoltagePowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            BatteryState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            CabPowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            firstUpdate = false;
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            CarId = Train?.Cars.IndexOf(Locomotive) ?? 0;

            if (firstUpdate)
            {
                firstUpdate = false;

                TrainCar previousCar = CarId > 0 ? Train.Cars[CarId - 1] : null;

                // Connect the power supply cable if the previous car is a locomotive or another passenger car
                if (previousCar != null
                    && (previousCar.WagonType == TrainCar.WagonTypes.Engine
                        || previousCar.WagonType == TrainCar.WagonTypes.Passenger)
                    )
                {
                    FrontElectricTrainSupplyCableConnected = true;
                }
            }

            Battery.Update(elapsedClockSeconds);
            MasterKey.Update(elapsedClockSeconds);
            ElectricTrainSupplySwitch.Update(elapsedClockSeconds);
        }

        public PowerSupplyState GetPowerStatus() => AbstractScript?.GetPowerStatus() ?? PowerSupplyState.Unavailable;

        public void HandleEvent(PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEvent(evt, id);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEventFromTcs(evt);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEventFromTcs(evt, id);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt, string message)
        {
            AbstractScript?.HandleEventFromTcs(evt, message);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEventFromLeadLocomotive(evt);
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEventFromLeadLocomotive(evt, id);
        }

        public void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt)
        {
            AbstractScript?.HandleEventFromOtherLocomotive(locoIndex, evt);
        }

        public void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt, int id)
        {
            AbstractScript?.HandleEventFromOtherLocomotive(locoIndex, evt, id);
        }

        protected virtual void AssignScriptFunctions()
        {
            // AbstractScriptClass
            AbstractScript.ClockTime = () => (float)Simulator.ClockTime;
            AbstractScript.GameTime = () => (float)Simulator.GameTime;
            AbstractScript.PreUpdate = () => Simulator.PreUpdate;
            AbstractScript.DistanceM = () => Locomotive.DistanceM;
            AbstractScript.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
            AbstractScript.Confirm = Locomotive.Simulator.Confirmer.Confirm;
            AbstractScript.Message = Locomotive.Simulator.Confirmer.Message;
            AbstractScript.SignalEvent = Locomotive.SignalEvent;
            AbstractScript.SignalEventToTrain = (evt) => Locomotive.Train?.SignalEvent(evt);
        }
        
        // Converts the generic string (e.g. ORTS_POWER_SUPPLY5) shown when browsing with the mouse on a PowerSupply control
        // to a customized string defined in the script
        public string GetDisplayString(int commandIndex)
        {
            if (CustomizedCabviewControlNames.TryGetValue(commandIndex - 1, out string name)) return name;
            return "ORTS_POWER_SUPPLY"+commandIndex;
        }
    }

}
