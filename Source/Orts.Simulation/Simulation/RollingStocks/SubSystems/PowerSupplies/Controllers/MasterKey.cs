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
using System.IO;
using System.Linq;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class MasterKey : ISubSystem<MasterKey>
    {
        // Enums
        public enum ModeType
        {
            AlwaysOn,
            Manual
        }
        
        // Parameters
        public ModeType Mode { get; protected set; } = ModeType.AlwaysOn;
        public float DelayS { get; protected set; } = 0f;
        public bool HeadlightControl = false;

        // Variables
        readonly MSTSLocomotive Locomotive;
        protected Timer Timer;
        public bool CommandSwitch { get; protected set; } = false;
        public bool On { get; protected set; } = false;
        public bool OtherCabInUse {
            get
            {
                foreach (TrainCar car in Locomotive.Train.Cars)
                {
                    if (car is MSTSLocomotive locomotive && locomotive != Locomotive && locomotive.LocomotivePowerSupply.MasterKey.On)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public MasterKey(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;

            Timer = new Timer(Locomotive);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsmasterkey(mode":
                    string text = stf.ReadStringBlock("").ToLower();
                    if (text == "alwayson")
                    {
                        Mode = ModeType.AlwaysOn;
                    }
                    else if (text == "manual")
                    {
                        Mode = ModeType.Manual;
                    }
                    else
                    {
                        STFException.TraceWarning(stf, "Skipped invalid master key mode");
                    }
                    break;

                case "engine(ortsmasterkey(delayoff":
                    DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0f);
                    break;

                case "engine(ortsmasterkey(headlightcontrol":
                    HeadlightControl = stf.ReadBoolBlock(false);
                    break;
            }
        }

        public void Copy(MasterKey other)
        {
            Mode = other.Mode;
            DelayS = other.DelayS;
            HeadlightControl = other.HeadlightControl;
        }

        public virtual void Initialize()
        {
            Timer.Setup(DelayS);
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
            if (Locomotive.IsLeadLocomotive())
            {
                CommandSwitch = true;
                On = true;
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
                case ModeType.AlwaysOn:
                    On = true;
                    break;

                case ModeType.Manual:
                    if (On)
                    {
                        if (!CommandSwitch)
                        {
                            if (!Timer.Started)
                            {
                                Timer.Start();
                            }

                            if (Timer.Triggered)
                            {
                                On = false;
                                Timer.Stop();
                            }
                        }
                        else
                        {
                            if (Timer.Started)
                            {
                                Timer.Stop();
                            }
                        }
                    }
                    else
                    {
                        if (CommandSwitch)
                        {
                            On = true;
                        }
                    }
                    break;
            }

            if (HeadlightControl)
            {
                if (On && Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn && Locomotive.Headlight == 0)
                {
                    Locomotive.Headlight = 1;
                }
                else if ((!On || !Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn) && Locomotive.Headlight > 0)
                {
                    Locomotive.Headlight = 0;
                }
            }
        }

        public virtual void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.TurnOnMasterKey:
                    if (Mode == ModeType.Manual)
                    {
                        CommandSwitch = true;
                        Locomotive.SignalEvent(Event.MasterKeyOn);
                    }
                    break;

                case PowerSupplyEvent.TurnOffMasterKey:
                    if (Mode == ModeType.Manual)
                    {
                        CommandSwitch = false;
                        Locomotive.SignalEvent(Event.MasterKeyOff);
                    }
                    break;
            }
        }
    }
}
