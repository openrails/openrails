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
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class ElectricTrainSupplySwitch : ISubSystem<ElectricTrainSupplySwitch>
    {
        // Parameters
        public enum ModeType
        {
            Automatic, // Locomotive with automatic ETS activation
            Unfitted, // Locomotive without ETS
            Switch,    // Locomotive with ETS activation via a switch
        }
        public ModeType Mode { get; protected set; }

        // Variables
        readonly MSTSLocomotive Locomotive;
        public bool CommandSwitch { get; protected set; } = false;
        public bool On { get; protected set; } = false;
        public bool QuickPowerOn = false;
        private bool firstUpdate = true;

        /// <summary>
        /// Number of cars that require energy from the electric train supply
        /// </summary>
        protected int NumberOfElectricTrainSupplyConnectedCars
        {
            get
            {
                int count = 0;
                foreach (var car in Locomotive.Train.Cars)
                {
                    if (car == null) continue;
                    if (!(car is MSTSWagon wagon)) continue;
                    if (!(wagon.PassengerCarPowerSupply?.ElectricTrainSupplyConnectedLocomotives.Contains(Locomotive) ?? false)) continue;
                    count++;
                }
                return count;
            }
        }

        public ElectricTrainSupplySwitch(MSTSLocomotive locomotive, ModeType defaultMode = ModeType.Automatic)
        {
            Locomotive = locomotive;
            Mode = defaultMode;
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortselectrictrainsupply(mode":
                    string text = stf.ReadStringBlock("").ToLower();
                    if (text == "automatic")
                    {
                        Mode = ModeType.Automatic;
                    }
                    else if (text == "unfitted")
                    {
                        Mode = ModeType.Unfitted;
                    }
                    else if (text == "switch")
                    {
                        Mode = ModeType.Switch;
                    }
                    else
                    {
                        STFException.TraceWarning(stf, "Skipped invalid electric train supply switch mode");
                    }
                    break;
            }
        }

        public void Copy(ElectricTrainSupplySwitch other)
        {
            Mode = other.Mode;
            On = other.On;
        }

        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
            switch (Mode)
            {
                case ModeType.Unfitted:
                    CommandSwitch = false;
                    On = false;
                    break;

                default:
                    CommandSwitch = true;
                    On = true;
                    break;
            }
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(CommandSwitch);
            outf.Write(On);
        }

        public virtual void Restore(BinaryReader inf)
        {
            CommandSwitch = inf.ReadBoolean();
            On = inf.ReadBoolean();
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            switch (Mode)
            {
                case ModeType.Unfitted:
                    On = false;
                    break;

                case ModeType.Automatic:
                    if (On)
                    {
                        if (!Locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn || NumberOfElectricTrainSupplyConnectedCars == 0)
                        {
                            On = false;
                            Locomotive.SignalEvent(Event.ElectricTrainSupplyOff);
                        }
                    }
                    else
                    {
                        if (Locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn && NumberOfElectricTrainSupplyConnectedCars > 0)
                        {
                            On = true;
                            Locomotive.SignalEvent(Event.ElectricTrainSupplyOn);
                        }
                    }
                    break;

                case ModeType.Switch:
                    if (QuickPowerOn && !firstUpdate)
                    {
                        if (NumberOfElectricTrainSupplyConnectedCars == 0 || CommandSwitch)
                        {
                            QuickPowerOn = false;
                        }
                        else if (Locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn)
                        {
                            QuickPowerOn = false;
                            CommandSwitch = true;
                            Locomotive.SignalEvent(Event.ElectricTrainSupplyCommandOn);
                        }
                    }
                    if (On)
                    {
                        if (!CommandSwitch || !Locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn)
                        {
                            On = false;
                            Locomotive.SignalEvent(Event.ElectricTrainSupplyOff);
                        }
                    }
                    else
                    {
                        if (CommandSwitch && Locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn)
                        {
                            On = true;
                            Locomotive.SignalEvent(Event.ElectricTrainSupplyOn);
                        }
                    }
                    break;
            }
            firstUpdate = false;
        }

        public virtual void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    if (Mode == ModeType.Switch)
                    {
                        QuickPowerOn = true;
                    }
                    break;
                case PowerSupplyEvent.SwitchOnElectricTrainSupply:
                    if (Mode == ModeType.Switch)
                    {
                        CommandSwitch = true;
                        Locomotive.SignalEvent(Event.ElectricTrainSupplyCommandOn);
                    }
                    break;

                case PowerSupplyEvent.SwitchOffElectricTrainSupply:
                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    if (Mode == ModeType.Switch)
                    {
                        CommandSwitch = false;
                        Locomotive.SignalEvent(Event.ElectricTrainSupplyCommandOff);
                    }
                    break;
            }
        }
    }
}
