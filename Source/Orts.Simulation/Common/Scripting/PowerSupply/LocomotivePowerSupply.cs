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
using System.IO;
using System.Linq;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;

namespace ORTS.Scripting.Api
{
    public abstract class LocomotivePowerSupply : PowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedLocomotivePowerSupply LpsHost => Host as ScriptedLocomotivePowerSupply;
        internal MSTSLocomotive Locomotive => LpsHost.Locomotive;
        internal Simulator Simulator => Locomotive.Simulator;

        /// <summary>
        /// Returns true if the current locomotive is leading
        /// </summary>
        protected bool IsLocomotiveLeading => Locomotive.IsLeadLocomotive();

        /// <summary>
        /// Current state of the main power supply
        /// Main power comes from the pantograph or the diesel generator
        /// </summary>
        protected PowerSupplyState CurrentMainPowerSupplyState() => LpsHost.MainPowerSupplyState;

        /// <summary>
        /// Current state of the auxiliary power supply
        /// Auxiliary power is used by auxiliary systems of a locomotive (such as ventilation or air compressor) and by systems of the cars (such as air conditionning)
        /// </summary>
        protected PowerSupplyState CurrentAuxiliaryPowerSupplyState() => LpsHost.AuxiliaryPowerSupplyState;

        /// <summary>
        /// Current state of the cab power supply
        /// </summary>
        protected PowerSupplyState CurrentCabPowerSupplyState() => LpsHost.CabPowerSupplyState;

        /// <summary>
        /// Current state of the helper engines
        /// </summary>
        protected DieselEngineState CurrentHelperEnginesState()
        {
            DieselEngineState state = DieselEngineState.Unavailable;

            foreach (TrainCar car in Train.Cars)
            {
                if (car is MSTSDieselLocomotive locomotive && locomotive.RemoteControlGroup != -1)
                {
                    if (locomotive == Train.LeadLocomotive)
                    {
                        foreach (DieselEngine dieselEngine in locomotive.DieselEngines)
                        {
                            if (dieselEngine != locomotive.DieselEngines[0] && dieselEngine.State > state)
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
            }

            return state;
        }

        /// <summary>
        /// Power limit for traction motors
        /// </summary>
        protected float MaximumPowerW
        {
            get => LpsHost.MaximumPowerW;
            set => LpsHost.MaximumPowerW = value;
        }

        /// <summary>
        /// Power limit for traction motors
        /// </summary>
        protected float AvailableTractionPowerW
        {
            get => LpsHost.AvailableTractionPowerW;
            set => LpsHost.AvailableTractionPowerW = value;
        }

        /// <summary>
        /// Current availability of the dynamic brake
        /// </summary>
        protected bool CurrentDynamicBrakeAvailability() => LpsHost.DynamicBrakeAvailable;

        /// <summary>
        /// Maximum dynamic braking power of the locomotive
        /// </summary>
        protected float MaximumDynamicBrakePowerW
        {
            get => LpsHost.MaximumDynamicBrakePowerW;
            set => LpsHost.MaximumDynamicBrakePowerW = value;
        }

        /// <summary>
        /// Dynamic brake percent demanded by power supply
        /// </summary>
        protected float PowerSupplyDynamicBrakePercent
        {
            get
            {
                return LpsHost.PowerSupplyDynamicBrakePercent;
            }
            set
            {
                if (value < 0) LpsHost.PowerSupplyDynamicBrakePercent = -1;
                else LpsHost.PowerSupplyDynamicBrakePercent = Math.Min(value, 100);
            }
        }

        /// <summary>
        /// Maximum throttle percent allowed by power supply
        /// </summary>
        protected float MaxThrottlePercent
        {
            get
            {
                return LpsHost.MaxThrottlePercent;
            }
            set
            {
                LpsHost.MaxThrottlePercent = Math.Max(Math.Min(value, 100), 0);
            }
        }

        /// <summary>
        /// Reduction of throttle percent enforced by power supply
        /// </summary>
        protected float ThrottleReductionPercent
        {
            get
            {
                return LpsHost.ThrottleReductionPercent;
            }
            set
            {
                LpsHost.ThrottleReductionPercent = Math.Max(Math.Min(value, 100), 0);
            }
        }

        /// <summary>
        /// Current throttle percentage
        /// </summary>
        protected float ThrottlePercent() => Locomotive.ThrottlePercent;

        /// <summary>
        /// Locomotive direction.
        /// </summary>
        protected Direction CurrentDirection() => Locomotive.Direction;

        /// <summary>
        /// Main supply power on delay
        /// </summary>
        protected float PowerOnDelayS() => LpsHost.PowerOnDelayS;

        /// <summary>
        /// Auxiliary supply power on delay
        /// </summary>
        protected float AuxPowerOnDelayS() => LpsHost.AuxPowerOnDelayS;

        /// <summary>
        /// True if the master key is switched on
        /// </summary>
        protected bool MasterKeyOn() => LpsHost.MasterKey.On;

        /// <summary>
        /// True if the electric train supply is switched on
        /// </summary>
        protected bool ElectricTrainSupplySwitchOn() => LpsHost.ElectricTrainSupplySwitch.On;

        /// <summary>
        /// True if the locomotive is not fitted with electric train supply
        /// </summary>
        protected bool ElectricTrainSupplyUnfitted() => LpsHost.ElectricTrainSupplySwitch.Mode == ElectricTrainSupplySwitch.ModeType.Unfitted;


        /// <summary>
        /// Power demanded by electric train supply
        /// </summary>
        protected float ElectricTrainSupplyPowerW => LpsHost.ElectricTrainSupplyPowerW;

        /// <summary>
        /// Returns the index of the current locomotive in the train (taking into account only locomotives)
        /// </summary>
        public int IndexOfLocomotive()
        {
            int count=0;
            for (int i=0; i<Train.Cars.Count; i++)
            {
                if (Train.Cars[i] is MSTSLocomotive)
                {
                    if (Train.Cars[i] == Locomotive) return count;
                    count++;
                }
            }
            return -1;
        }

        /// <summary>
        /// True if the service retention button is pressed
        /// </summary>
        protected bool ServiceRetentionButton
        {
            get => LpsHost.ServiceRetentionButton;
            set => LpsHost.ServiceRetentionButton = value;
        }

        /// <summary>
        /// True if the service retention cancellation button is pressed
        /// </summary>
        protected bool ServiceRetentionCancellationButton
        {
            get => LpsHost.ServiceRetentionCancellationButton;
            set => LpsHost.ServiceRetentionCancellationButton = value;
        }

        /// <summary>
        /// True if the service retention is active
        /// </summary>
        protected bool ServiceRetentionActive
        {
            get => LpsHost.ServiceRetentionActive;
            set => LpsHost.ServiceRetentionActive = value;
        }
        /// <summary>
        /// Sets the value for a cabview control.
        /// </summary>
        protected void SetCabDisplayControl(int index, float value)
        {
            LpsHost.CabDisplayControls[index] = value;
        }

        /// <summary>
        /// Sets the name which is to be shown which putting the cursor above a cabview control.
        /// </summary>
        protected void SetCustomizedCabviewControlName(int index, string name)
        {
            LpsHost.CustomizedCabviewControlNames[index] = name;
        }

        /// <summary>
        /// Sets the current state of the main power supply (power from the pantograph or the generator)
        /// Main power comes from the pantograph or the diesel generator
        /// </summary>
        protected virtual void SetCurrentMainPowerSupplyState(PowerSupplyState state) => LpsHost.MainPowerSupplyState = state;

        /// <summary>
        /// Sets the current state of the auxiliary power supply
        /// Auxiliary power is used by auxiliary systems of a locomotive (such as ventilation or air compressor) and by systems of the cars (such as air conditionning)
        /// </summary>
        protected virtual void SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState state) => LpsHost.AuxiliaryPowerSupplyState = state;

        /// <summary>
        /// Sets the current state of the cab power supply
        /// </summary>
        protected void SetCurrentCabPowerSupplyState(PowerSupplyState state) => LpsHost.CabPowerSupplyState = state;

        /// <summary>
        /// Sets the current state of the electric train supply
        /// ETS is used by the systems of the cars (such as air conditionning)
        /// </summary>
        protected virtual void SetCurrentElectricTrainSupplyState(PowerSupplyState state) => LpsHost.ElectricTrainSupplyState = state;
        /// <summary>
        /// Sets the current availability of the dynamic brake
        /// </summary>
        protected virtual void SetCurrentDynamicBrakeAvailability(bool avail) => LpsHost.DynamicBrakeAvailable = avail;

        /// <summary>
        /// Called by other subsystems to determine whether the locomotive is powered
        /// </summary>
        /// <returns>
        /// PowerSupplyState.PowerOff if the locomotive is unpowered
        /// PowerSupplyState.PowerOnOngoing if the locomotive is in a power on sequence, but not all subsystems are ready
        /// PowerSupplyState.PowerOn if the locomotive is ready for service (all necessary subystems are connected)
        /// </returns>
        public virtual PowerSupplyState GetPowerStatus()
        {
            if (LpsHost.MainPowerSupplyState == LpsHost.AuxiliaryPowerSupplyState) return LpsHost.MainPowerSupplyState;
            return PowerSupplyState.PowerOnOngoing;
        }

        /// <summary>
        /// Sends an event to the master switch
        /// </summary>
        protected void SignalEventToMasterKey(PowerSupplyEvent evt) => LpsHost.MasterKey.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the electric train supply switch
        /// </summary>
        protected void SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent evt) => LpsHost.ElectricTrainSupplySwitch.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the train control system
        /// </summary>
        protected void SignalEventToTcs(PowerSupplyEvent evt) => Locomotive.TrainControlSystem.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the train control system with a message
        /// </summary>
        protected void SignalEventToTcsWithMessage(PowerSupplyEvent evt, string message) => Locomotive.TrainControlSystem.HandleEvent(evt, message);

        /// <summary>
        /// Sends an event to the power supplies of other locomotives
        /// </summary>
        protected void SignalEventToOtherLocomotives(PowerSupplyEvent evt)
        {
            if (Locomotive == Train.LeadLocomotive || (Train.LeadLocomotive == null && IndexOfLocomotive() == 0))
            {
                foreach (TrainCar car in Train.Cars)
                {
                    if (car is MSTSLocomotive locomotive && locomotive != Locomotive && locomotive.RemoteControlGroup != -1)
                    {
                        locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt);
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supplies of other locomotives
        /// </summary>
        protected void SignalEventToOtherLocomotivesWithId(PowerSupplyEvent evt, int id)
        {
            if (Locomotive == Train.LeadLocomotive || (Train.LeadLocomotive == null && IndexOfLocomotive() == 0))
            {
                foreach (TrainCar car in Train.Cars)
                {
                    if (car is MSTSLocomotive locomotive && locomotive != Locomotive && locomotive.RemoteControlGroup != -1)
                    {
                        locomotive.LocomotivePowerSupply.HandleEventFromLeadLocomotive(evt, id);
                    }
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supplies of other train vehicles
        /// </summary>
        protected void SignalEventToOtherTrainVehicles(PowerSupplyEvent evt)
        {
            if (Locomotive == Train.LeadLocomotive || (Train.LeadLocomotive == null && IndexOfLocomotive() == 0))
            {
                foreach (TrainCar car in Train.Cars)
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
        protected void SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent evt, int id)
        {
            if (Locomotive == Train.LeadLocomotive || (Train.LeadLocomotive == null && IndexOfLocomotive() == 0))
            {
                foreach (TrainCar car in Train.Cars)
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
        /// Sends an event to the power supply of the lead locomotive
        /// </summary>
        protected void SignalEventToLeadLocomotive(PowerSupplyEvent evt)
        {
            (Train.LeadLocomotive as MSTSLocomotive)?.LocomotivePowerSupply.HandleEventFromOtherLocomotive(IndexOfLocomotive(), evt);
        }

        /// <summary>
        /// Sends an event to the power supply of the lead locomotive
        /// </summary>
        protected void SignalEventToLeadLocomotive(PowerSupplyEvent evt, int id)
        {
            (Train.LeadLocomotive as MSTSLocomotive)?.LocomotivePowerSupply.HandleEventFromOtherLocomotive(IndexOfLocomotive(), evt, id);
        }

        /// <summary>
        /// Sends an event to the power supply of specific locomotive
        /// </summary>
        protected void SignalEventToOtherLocomotive(int locoIndex, PowerSupplyEvent evt)
        {
            int count=0;
            for (int i=0; i<Train.Cars.Count; i++)
            {
                if (Train.Cars[i] is MSTSLocomotive loco)
                {
                    if (count == locoIndex) loco.LocomotivePowerSupply.HandleEventFromOtherLocomotive(IndexOfLocomotive(), evt);
                    count++;
                }
            }
        }

        /// <summary>
        /// Sends an event to the power supply of specific locomotive
        /// </summary>
        protected void SignalEventToOtherLocomotive(int locoIndex, PowerSupplyEvent evt, int id)
        {
            int count=0;
            for (int i=0; i<Train.Cars.Count; i++)
            {
                if (Train.Cars[i] is MSTSLocomotive loco)
                {
                    if (count == locoIndex) loco.LocomotivePowerSupply.HandleEventFromOtherLocomotive(IndexOfLocomotive(), evt, id);
                    count++;
                }
            }
        }

        /// <summary>
        /// Sends an event to all helper engines
        /// </summary>
        protected void SignalEventToHelperEngines(PowerSupplyEvent evt)
        {
            bool helperFound = false; //this avoids that locomotive engines toggle in opposite directions

            foreach (TrainCar car in Train.Cars)
            {
                if (car is MSTSDieselLocomotive locomotive && locomotive.RemoteControlGroup != -1)
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

        public virtual void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt)
        {

        }

        public virtual void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt, int id)
        {

        }

        /// <summary>
        /// Called when the TCS wants to transmit an event to power supply
        /// </summary>
        /// <param name="evt"></param>
        public virtual void HandleEventFromTcs(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.LowerPantograph:
                case PowerSupplyEvent.RaisePantograph:
                    SignalEventToPantographs(evt);
                    SignalEventToOtherTrainVehicles(evt);
                    break;
            }
        }

        /// <summary>
        /// Called when the TCS wants to transmit an event to power supply
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="id"></param>
        public virtual void HandleEventFromTcs(PowerSupplyEvent evt, int id)
        {
            switch (evt)
            {
                case PowerSupplyEvent.LowerPantograph:
                case PowerSupplyEvent.RaisePantograph:
                    SignalEventToPantograph(evt, id);
                    SignalEventToOtherTrainVehiclesWithId(evt, id);
                    break;
            }
        }

        /// <summary>
        /// Called when the TCS wants to transmit an event and/or a message to power supply
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="message"></param>
        public virtual void HandleEventFromTcs(PowerSupplyEvent evt, string message) {}

        protected bool GetBoolParameter(string sectionName, string keyName, bool defaultValue) => LoadParameter(sectionName, keyName, defaultValue);
        protected int GetIntParameter(string sectionName, string keyName, int defaultValue) => LoadParameter(sectionName, keyName, defaultValue);
        protected float GetFloatParameter(string sectionName, string keyName, float defaultValue) => LoadParameter(sectionName, keyName, defaultValue);
        protected string GetStringParameter(string sectionName, string keyName, string defaultValue) => LoadParameter(sectionName, keyName, defaultValue);

        protected T LoadParameter<T>(string sectionName, string keyName, T defaultValue)
        {
            string buffer;
            int length;

            if (File.Exists(LpsHost.ParametersFileName))
            {
                buffer = new string('\0', 256);
                length = NativeMethods.GetPrivateProfileString(sectionName, keyName, null, buffer, buffer.Length, LpsHost.ParametersFileName);

                if (length > 0)
                {
                    buffer = buffer.Trim('\0').Trim();
                    return (T)Convert.ChangeType(buffer, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return defaultValue;
        }
    }
}
