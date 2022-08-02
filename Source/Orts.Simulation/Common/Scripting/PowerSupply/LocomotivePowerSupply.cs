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

using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using System;
using System.Linq;

namespace ORTS.Scripting.Api
{
    public abstract class LocomotivePowerSupply : PowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedLocomotivePowerSupply LpsHost => Host as ScriptedLocomotivePowerSupply;
        internal MSTSLocomotive Locomotive => LpsHost.Locomotive;
        internal Train Train => Locomotive.Train;
        internal Simulator Simulator => Locomotive.Simulator;

        /// <summary>
        /// Current state of the main power supply
        /// Main power comes from the pantograph or the diesel generator
        /// </summary>
        public Func<PowerSupplyState> CurrentMainPowerSupplyState;
        /// <summary>
        /// Current state of the auxiliary power supply
        /// Auxiliary power is used by auxiliary systems of a locomotive (such as ventilation or air compressor) and by systems of the cars (such as air conditionning)
        /// </summary>
        public Func<PowerSupplyState> CurrentAuxiliaryPowerSupplyState;
        /// <summary>
        /// Current state of the cab power supply
        /// </summary>
        public Func<PowerSupplyState> CurrentCabPowerSupplyState;
        /// <summary>
        /// Current state of the helper engines
        /// </summary>
        public Func<DieselEngineState> CurrentHelperEnginesState;
        /// <summary>
        /// Current availability of the dynamic brake
        /// </summary>
        public Func<bool> CurrentDynamicBrakeAvailability;
        /// <summary>
        /// Current throttle percentage
        /// </summary>
        public Func<float> ThrottlePercent;
        /// <summary>
        /// Main supply power on delay
        /// </summary>
        public Func<float> PowerOnDelayS;
        /// <summary>
        /// Auxiliary supply power on delay
        /// </summary>
        public Func<float> AuxPowerOnDelayS;
        /// <summary>
        /// True if the master key is switched on
        /// </summary>
        public Func<bool> MasterKeyOn;
        /// <summary>
        /// True if the electric train supply is switched on
        /// </summary>
        public Func<bool> ElectricTrainSupplySwitchOn;
        /// <summary>
        /// True if the locomotive is not fitted with electric train supply
        /// </summary>
        public Func<bool> ElectricTrainSupplyUnfitted;

        /// <summary>
        /// Sets the current state of the main power supply (power from the pantograph or the generator)
        /// Main power comes from the pantograph or the diesel generator
        /// </summary>
        public Action<PowerSupplyState> SetCurrentMainPowerSupplyState;
        /// <summary>
        /// Sets the current state of the auxiliary power supply
        /// Auxiliary power is used by auxiliary systems of a locomotive (such as ventilation or air compressor) and by systems of the cars (such as air conditionning)
        /// </summary>
        public Action<PowerSupplyState> SetCurrentAuxiliaryPowerSupplyState;
        /// <summary>
        /// Sets the current state of the cab power supply
        /// </summary>
        public Action<PowerSupplyState> SetCurrentCabPowerSupplyState;
        /// <summary>
        /// Sets the current state of the electric train supply
        /// ETS is used by the systems of the cars (such as air conditionning)
        /// </summary>
        public Action<PowerSupplyState> SetCurrentElectricTrainSupplyState;
        /// <summary>
        /// Sets the current availability of the dynamic brake
        /// </summary>
        public Action<bool> SetCurrentDynamicBrakeAvailability;

        /// <summary>
        /// Sends an event to the master switch
        /// </summary>
        public void SignalEventToMasterKey(PowerSupplyEvent evt) => LpsHost.MasterKey.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the electric train supply switch
        /// </summary>
        public void SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent evt) => LpsHost.ElectricTrainSupplySwitch.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the train control system
        /// </summary>
        public void SignalEventToTcs(PowerSupplyEvent evt) => LpsHost.Locomotive.TrainControlSystem.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the train control system with a message
        /// </summary>
        public void SignalEventToTcsWithMessage(PowerSupplyEvent evt, string message) => LpsHost.Locomotive.TrainControlSystem.HandleEvent(evt, message);

        /// <summary>
        /// Sends an event to the power supplies of other locomotives
        /// </summary>
        public void SignalEventToOtherLocomotives(PowerSupplyEvent evt)
        {
            if (Locomotive == Train.LeadLocomotive)
            {
                foreach (MSTSLocomotive locomotive in Locomotive.Train.Cars.OfType<MSTSLocomotive>())
                {
                    if (locomotive != Locomotive && locomotive.RemoteControlGroup != -1)
                    {
                        locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt);
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supplies of other locomotives
        /// </summary>
        public void SignalEventToOtherLocomotivesWithId(PowerSupplyEvent evt, int id)
        {
            if (Locomotive == Train.LeadLocomotive)
            {
                foreach (MSTSLocomotive locomotive in Locomotive.Train.Cars.OfType<MSTSLocomotive>())
                {
                    if (locomotive != Locomotive && locomotive.RemoteControlGroup != -1)
                    {
                        locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt, id);
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supplies of other train vehicles
        /// </summary>
        public void SignalEventToOtherTrainVehicles(PowerSupplyEvent evt)
        {
            if (Locomotive == Train.LeadLocomotive)
            {
                foreach (TrainCar car in Locomotive.Train.Cars)
                {
                    if (car != Locomotive && car.RemoteControlGroup != -1)
                    {
                        if (car.PowerSupply != null)
                        {
                            car.PowerSupply.HandleEventFromLeadLocomotive(evt);
                        }
                        else if (car is MSTSWagon wagon)
                        {
                            wagon.Pantographs.HandleEvent(evt);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supplies of other train vehicles
        /// </summary>
        public void SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent evt, int id)
        {
            if (Locomotive == Train.LeadLocomotive)
            {
                foreach (TrainCar car in Locomotive.Train.Cars)
                {
                    if (car != Locomotive && car.RemoteControlGroup != -1)
                    {
                        if (car.PowerSupply != null)
                        {
                            car.PowerSupply.HandleEventFromLeadLocomotive(evt, id);
                        }
                        else if (car is MSTSWagon wagon)
                        {
                            wagon.Pantographs.HandleEvent(evt, id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to all helper engines
        /// </summary>
        public void SignalEventToHelperEngines(PowerSupplyEvent evt)
        {
            bool helperFound = false; //this avoids that locomotive engines toggle in opposite directions

            foreach (MSTSDieselLocomotive locomotive in Train.Cars.OfType<MSTSDieselLocomotive>().Where((locomotive) => locomotive.RemoteControlGroup != -1))
            {
                if (locomotive == Train.LeadLocomotive)
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
        }

        /// <summary>
        /// Called when the driver (or the train's systems) want something to happen on the power supply system
        /// </summary>
        /// <param name="evt">The event</param>
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.ToggleHelperEngine:
                    switch (CurrentHelperEnginesState())
                    {
                        case DieselEngineState.Stopped:
                        case DieselEngineState.Stopping:
                            SignalEventToHelperEngines(PowerSupplyEvent.StartEngine);
                            Confirm(CabControl.HelperDiesel, CabSetting.On);
                            break;

                        case DieselEngineState.Starting:
                        case DieselEngineState.Running:
                            SignalEventToHelperEngines(PowerSupplyEvent.StopEngine);
                            Confirm(CabControl.HelperDiesel, CabSetting.Off);
                            break;
                    }
                    break;

                default:
                    base.HandleEvent(evt);

                    // By default, send the event to every component
                    SignalEventToMasterKey(evt);
                    SignalEventToElectricTrainSupplySwitch(evt);
                    SignalEventToTcs(evt);
                    SignalEventToOtherTrainVehicles(evt);
                    break;
            }
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            base.HandleEvent(evt, id);

            // By default, send the event to every component
            SignalEventToOtherTrainVehiclesWithId(evt, id);
        }

        public override void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            base.HandleEventFromLeadLocomotive(evt);

            // By default, send the event to every component
            SignalEventToElectricTrainSupplySwitch(evt);
            SignalEventToTcs(evt);
        }
    }
}
