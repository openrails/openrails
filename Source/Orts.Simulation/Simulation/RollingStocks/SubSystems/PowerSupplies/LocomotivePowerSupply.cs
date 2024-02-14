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

        public BatterySwitch BatterySwitch { get; protected set; }
        public MasterKey MasterKey { get; protected set; }
        public ElectricTrainSupplySwitch ElectricTrainSupplySwitch { get; protected set; }

        public abstract PowerSupplyType Type { get; }
        protected string ScriptName = "Default";
        protected LocomotivePowerSupply AbstractScript;

        public PowerSupplyState MainPowerSupplyState { get; protected set; } = PowerSupplyState.PowerOff;
        public bool MainPowerSupplyOn => MainPowerSupplyState == PowerSupplyState.PowerOn;
        public bool DynamicBrakeAvailable { get; protected set; } = false;

        public PowerSupplyState AuxiliaryPowerSupplyState { get; protected set; } = PowerSupplyState.PowerOff;
        public bool AuxiliaryPowerSupplyOn => AuxiliaryPowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState ElectricTrainSupplyState { get; protected set; } = PowerSupplyState.PowerOff;
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
                    result += wagon.PassengerCarPowerSupply.ElectricTrainSupplyPowerW / wagon.PassengerCarPowerSupply.ElectricTrainSupplyConnectedLocomotives.Count();
                }
                return result;
            }
        }

        public PowerSupplyState LowVoltagePowerSupplyState { get; protected set; } = PowerSupplyState.PowerOff;
        public bool LowVoltagePowerSupplyOn => LowVoltagePowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState BatteryState { get; protected set; } = PowerSupplyState.PowerOff;
        public bool BatteryOn => BatteryState == PowerSupplyState.PowerOn;

        public PowerSupplyState CabPowerSupplyState { get; protected set; } = PowerSupplyState.PowerOff;
        public bool CabPowerSupplyOn => CabPowerSupplyState == PowerSupplyState.PowerOn;

        public float PowerOnDelayS { get; protected set; } = 0f;
        public float AuxPowerOnDelayS { get; protected set; } = 0f;

        public bool ServiceRetentionButton { get; protected set; } = false;
        public bool ServiceRetentionCancellationButton { get; protected set; } = false;

        private bool firstUpdate = true;

        protected ScriptedLocomotivePowerSupply(MSTSLocomotive locomotive)
        {
            Car = locomotive;

            BatterySwitch = new BatterySwitch(Locomotive);
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

                case "engine(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsauxpowerondelay":
                    AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsbattery(mode":
                case "engine(ortsbattery(delay":
                case "engine(ortsbattery(defaulton":
                    BatterySwitch.Parse(lowercasetoken, stf);
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
                BatterySwitch.Copy(scriptedOther.BatterySwitch);
                MasterKey.Copy(scriptedOther.MasterKey);
                ElectricTrainSupplySwitch.Copy(scriptedOther.ElectricTrainSupplySwitch);

                ScriptName = scriptedOther.ScriptName;

                PowerOnDelayS = scriptedOther.PowerOnDelayS;
                AuxPowerOnDelayS = scriptedOther.AuxPowerOnDelayS;
            }
        }

        public virtual void Initialize()
        {
            BatterySwitch.Initialize();
            MasterKey.Initialize();
            ElectricTrainSupplySwitch.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public virtual void InitializeMoving()
        {
            BatterySwitch.InitializeMoving();
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
            BatterySwitch.Save(outf);
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
            BatterySwitch.Restore(inf);
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

            BatterySwitch.Update(elapsedClockSeconds);
            MasterKey.Update(elapsedClockSeconds);
            ElectricTrainSupplySwitch.Update(elapsedClockSeconds);
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
            AbstractScript.DistanceM = () => Locomotive.DistanceM;
            AbstractScript.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
            AbstractScript.Confirm = Locomotive.Simulator.Confirmer.Confirm;
            AbstractScript.Message = Locomotive.Simulator.Confirmer.Message;
            AbstractScript.SignalEvent = Locomotive.SignalEvent;
            AbstractScript.SignalEventToTrain = (evt) =>
            {
                if (Locomotive.Train != null)
                {
                    Locomotive.Train.SignalEvent(evt);
                }
            };

            // AbstractPowerSupply getters
            AbstractScript.CurrentMainPowerSupplyState = () => MainPowerSupplyState;
            AbstractScript.CurrentAuxiliaryPowerSupplyState = () => AuxiliaryPowerSupplyState;
            AbstractScript.CurrentElectricTrainSupplyState = () => ElectricTrainSupplyState;
            AbstractScript.CurrentLowVoltagePowerSupplyState = () => LowVoltagePowerSupplyState;
            AbstractScript.CurrentBatteryState = () => BatteryState;
            AbstractScript.CurrentCabPowerSupplyState = () => CabPowerSupplyState;
            AbstractScript.CurrentHelperEnginesState = () =>
            {
                DieselEngineState state = DieselEngineState.Unavailable;

                foreach (MSTSDieselLocomotive locomotive in Train.Cars.OfType<MSTSDieselLocomotive>().Where((MSTSLocomotive locomotive) => { return locomotive.RemoteControlGroup != -1; }))
                {
                    if (locomotive == Simulator.PlayerLocomotive)
                    {
                        foreach (DieselEngine dieselEngine in locomotive.DieselEngines.DEList.Where(de => de != locomotive.DieselEngines[0]))
                        {
                            if (dieselEngine.State > state)
                                state = dieselEngine.State;
                        }
                    }
                    else
                    {
                        foreach (DieselEngine dieselEngine in locomotive.DieselEngines)
                        {
                            if (dieselEngine.State > state)
                                state = dieselEngine.State;
                        }
                    }
                }

                return state;
            };
            AbstractScript.CurrentDynamicBrakeAvailability = () => DynamicBrakeAvailable;
            AbstractScript.ThrottlePercent = () => Locomotive.ThrottlePercent;
            AbstractScript.PowerOnDelayS = () => PowerOnDelayS;
            AbstractScript.AuxPowerOnDelayS = () => AuxPowerOnDelayS;
            AbstractScript.BatterySwitchOn = () => BatterySwitch.On;
            AbstractScript.MasterKeyOn = () => MasterKey.On;
            AbstractScript.ElectricTrainSupplySwitchOn = () => ElectricTrainSupplySwitch.On;
            AbstractScript.ElectricTrainSupplyUnfitted = () => ElectricTrainSupplySwitch.Mode == ElectricTrainSupplySwitch.ModeType.Unfitted;

            // AbstractPowerSupply setters
            AbstractScript.SetCurrentMainPowerSupplyState = (value) => MainPowerSupplyState = value;
            AbstractScript.SetCurrentAuxiliaryPowerSupplyState = (value) => AuxiliaryPowerSupplyState = value;
            AbstractScript.SetCurrentElectricTrainSupplyState = (value) => ElectricTrainSupplyState = value;
            AbstractScript.SetCurrentLowVoltagePowerSupplyState = (value) => LowVoltagePowerSupplyState = value;
            AbstractScript.SetCurrentBatteryState = (value) => BatteryState = value;
            AbstractScript.SetCurrentCabPowerSupplyState = (value) => CabPowerSupplyState = value;
            AbstractScript.SetCurrentDynamicBrakeAvailability = (value) => DynamicBrakeAvailable = value;
            AbstractScript.SignalEventToBatterySwitch = (evt) => BatterySwitch.HandleEvent(evt);
            AbstractScript.SignalEventToMasterKey = (evt) => MasterKey.HandleEvent(evt);
            AbstractScript.SignalEventToElectricTrainSupplySwitch = (evt) => ElectricTrainSupplySwitch.HandleEvent(evt);
            AbstractScript.SignalEventToPantographs = (evt) => Locomotive.Pantographs.HandleEvent(evt);
            AbstractScript.SignalEventToPantograph = (evt, id) => Locomotive.Pantographs.HandleEvent(evt, id);
            AbstractScript.SignalEventToTcs = (evt) => Locomotive.TrainControlSystem.HandleEvent(evt);
            AbstractScript.SignalEventToTcsWithMessage = (evt, message) => Locomotive.TrainControlSystem.HandleEvent(evt, message);
            AbstractScript.SignalEventToOtherLocomotives = (evt) =>
            {
                if (Locomotive == Simulator.PlayerLocomotive)
                {
                    foreach (MSTSLocomotive locomotive in Locomotive.Train.Cars.OfType<MSTSLocomotive>())
                    {
                        if (locomotive != Locomotive && locomotive != Locomotive.Train.LeadLocomotive && locomotive.RemoteControlGroup != -1)
                        {
                            locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt);
                        }
                    }
                }
            };
            AbstractScript.SignalEventToOtherLocomotivesWithId = (evt, id) =>
            {
                if (Locomotive == Simulator.PlayerLocomotive)
                {
                    foreach (MSTSLocomotive locomotive in Locomotive.Train.Cars.OfType<MSTSLocomotive>())
                    {
                        if (locomotive != Locomotive && locomotive != Locomotive.Train.LeadLocomotive && locomotive.RemoteControlGroup != -1)
                        {
                            locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt, id);
                        }
                    }
                }
            };
            AbstractScript.SignalEventToOtherTrainVehicles = (evt) =>
            {
                if (Locomotive == Simulator.PlayerLocomotive)
                {
                    foreach (TrainCar car in Locomotive.Train.Cars)
                    {
                        if (car != Locomotive && car != Locomotive.Train.LeadLocomotive && car.RemoteControlGroup != -1)
                        {
                            car.PowerSupply?.HandleEventFromLeadLocomotive(evt);
                        }
                    }
                }
            };
            AbstractScript.SignalEventToOtherTrainVehiclesWithId = (evt, id) =>
            {
                if (Locomotive == Simulator.PlayerLocomotive)
                {
                    foreach (TrainCar car in Locomotive.Train.Cars)
                    {
                        if (car != Locomotive && car != Locomotive.Train.LeadLocomotive && car.RemoteControlGroup != -1)
                        {
                            car.PowerSupply?.HandleEventFromLeadLocomotive(evt, id);
                        }
                    }
                }
            };
            AbstractScript.SignalEventToHelperEngines = (evt) =>
            {
                bool helperFound = false; //this avoids that locomotive engines toggle in opposite directions

                foreach (MSTSDieselLocomotive locomotive in Train.Cars.OfType<MSTSDieselLocomotive>().Where((MSTSLocomotive locomotive) => { return locomotive.RemoteControlGroup != -1; }))
                {
                    if (locomotive == Simulator.PlayerLocomotive)
                    {
                        // Engine number 1 or above are helper engines
                        for (int i = 1; i < locomotive.DieselEngines.Count; i++)
                        {
                            if (!helperFound)
                            {
                                helperFound = true;
                            }

                            locomotive.DieselEngines.HandleEvent(evt, i);
                        }
                    }
                    else
                    {
                        if (!helperFound)
                        {
                            helperFound = true;
                        }

                        locomotive.DieselEngines.HandleEvent(evt);
                    }
                }

                if (helperFound && (evt == PowerSupplyEvent.StartEngine || evt == PowerSupplyEvent.StopEngine))
                {
                    Simulator.Confirmer.Confirm(CabControl.HelperDiesel, evt == PowerSupplyEvent.StartEngine ? CabSetting.On : CabSetting.Off);
                }
            };
        }
    }
}
