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

using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedControlCarPowerSupply : ScriptedLocomotivePowerSupply, ISubSystem<ScriptedControlCarPowerSupply>
    {
        public MSTSControlTrailerCar ControlTrailer => Locomotive as MSTSControlTrailerCar;

        public override PowerSupplyType Type => PowerSupplyType.ControlCar;

        protected int CarId = 0;


        public List<MSTSLocomotive> ElectricTrainSupplyConnectedLocomotives = new List<MSTSLocomotive>();
        public override PowerSupplyState ElectricTrainSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public override float ElectricTrainSupplyPowerW { get; set; } = 0f;

        private ControlCarPowerSupply Script => AbstractScript as ControlCarPowerSupply;

        private bool IsFirstUpdate = true;
        public ScriptedControlCarPowerSupply(MSTSControlTrailerCar controlcar) :
        base(controlcar)
        {

        }


        public void Copy(ScriptedControlCarPowerSupply other)
        {
            base.Copy(other);
        }
        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);

            outf.Write(FrontElectricTrainSupplyCableConnected);
            outf.Write(ElectricTrainSupplyState.ToString());
        }
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);

            FrontElectricTrainSupplyCableConnected = inf.ReadBoolean();
            ElectricTrainSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            IsFirstUpdate = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Script == null)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    string[] pathArray = { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as ControlCarPowerSupply;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Script == null)
                {
                    AbstractScript = new DefaultControlCarPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            Script?.InitializeMoving();
        }

        public override void Update(float elapsedClockSeconds)
        {
            CarId = Train?.Cars.IndexOf(Locomotive) ?? 0;

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
                    
                    if (connectedToLocomotive && locomotive.LocomotivePowerSupply.Type != PowerSupplyType.ControlCar) ElectricTrainSupplyConnectedLocomotives.Add(locomotive);
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

            base.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }
    }
    public class DefaultControlCarPowerSupply : ControlCarPowerSupply
    {
        private Timer AuxPowerOnTimer;
        public override void Initialize()
        {
            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
        }
        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentElectricTrainSupplyState())
            {
                case PowerSupplyState.PowerOff:
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();
                    if (CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOff)
                    {
                        SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.PowerConverterOff);
                    }
                    break;

                case PowerSupplyState.PowerOn:
                    if (!AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Start();
                    switch (CurrentAuxiliaryPowerSupplyState())
                    {
                        case PowerSupplyState.PowerOff:
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOnOngoing);
                            break;
                        case PowerSupplyState.PowerOnOngoing:
                            if (AuxPowerOnTimer.Triggered)
                            {
                                SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                                SignalEvent(Event.PowerConverterOn);
                            }
                            break;
                    }
                    break;
            }

        }
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                case PowerSupplyEvent.TogglePlayerEngine:
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
