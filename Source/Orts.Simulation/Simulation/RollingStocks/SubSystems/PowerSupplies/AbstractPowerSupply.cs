// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public abstract class AbstractPowerSupply
    {
        private readonly MSTSLocomotive Locomotive;
        private readonly Simulator Simulator;

        private PowerSupplyState state;
        public PowerSupplyState State
        {
            get
            {
                return state;
            }

            protected set
            {
                if (state != value)
                {
                    state = value;

                    switch (state)
                    {
                        case PowerSupplyState.PowerOff:
                            Locomotive.SignalEvent(Event.EnginePowerOff);
                            break;

                        case PowerSupplyState.PowerOn:
                            Locomotive.SignalEvent(Event.EnginePowerOn);
                            break;
                    }

                    Locomotive.PowerOn = PowerOn;
                }
            }
        }
        public bool PowerOn
        {
            get
            {
                return State == PowerSupplyState.PowerOn;
            }
        }

        private PowerSupplyState auxiliaryState;
        public PowerSupplyState AuxiliaryState
        {
            get
            {
                return auxiliaryState;
            }

            protected set
            {
                if (auxiliaryState != value)
                {
                    auxiliaryState = value;

                    if (Locomotive.Train != null && Locomotive.IsLeadLocomotive())
                    {
                        foreach (TrainCar car in Locomotive.Train.Cars)
                        {
                            MSTSWagon wagon = car as MSTSWagon;

                            if (wagon != null)
                            {
                                wagon.AuxPowerOn = AuxPowerOn;
                            }
                        }
                    }
                }
            }
        }
        public bool AuxPowerOn
        {
            get
            {
                return auxiliaryState == PowerSupplyState.PowerOn;
            }
        }

        public float PowerOnDelayS { get; private set; }
        public float AuxPowerOnDelayS { get; private set; }
        
        public AbstractPowerSupply(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = locomotive.Simulator;

            State = PowerSupplyState.PowerOff;
            AuxiliaryState = PowerSupplyState.PowerOff;
            PowerOnDelayS = 0;
            AuxPowerOnDelayS = 0;
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsauxpowerondelay":
                    AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;
            }
        }

        public void Copy(AbstractPowerSupply other)
        {
            State = other.State;
            AuxiliaryState = other.AuxiliaryState;

            PowerOnDelayS = other.PowerOnDelayS;
            AuxPowerOnDelayS = other.AuxPowerOnDelayS;
        }

        public virtual void Restore(BinaryReader inf)
        {
            State = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            AuxiliaryState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            PowerOnDelayS = inf.ReadSingle();
            AuxPowerOnDelayS = inf.ReadSingle();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public virtual void InitializeMoving()
        {
            State = PowerSupplyState.PowerOn;
            AuxiliaryState = PowerSupplyState.PowerOn;
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(State.ToString());
            outf.Write(AuxiliaryState.ToString());

            outf.Write(PowerOnDelayS);
            outf.Write(AuxPowerOnDelayS);
        }

    }

}
