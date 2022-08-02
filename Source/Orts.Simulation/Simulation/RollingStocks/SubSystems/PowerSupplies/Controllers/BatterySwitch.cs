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

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class BatterySwitch : ISubSystem<BatterySwitch>
    {
        // Parameters
        public enum ModeType
        {
            AlwaysOn,
            Switch,
            PushButtons,
        }
        public ModeType Mode { get; protected set; } = ModeType.AlwaysOn;
        public float DelayS { get; protected set; } = 0f;
        public bool DefaultOn { get; protected set; } = false;

        // Variables
        readonly MSTSWagon Wagon;
        protected Timer Timer;
        public bool CommandSwitch { get; protected set; } = false;
        public bool CommandButtonOn { get; protected set; } = false;
        public bool CommandButtonOff { get; protected set; } = false;
        public bool On { get; protected set; } = false;
        protected bool QuickPowerOn;
        protected bool QuickPowerOff;

        public BatterySwitch(MSTSWagon wagon)
        {
            Wagon = wagon;

            Timer = new Timer(Wagon);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsbattery(mode":
                case "wagon(ortsbattery(mode":
                    string text = stf.ReadStringBlock("").ToLower();
                    if (text == "alwayson")
                    {
                        Mode = ModeType.AlwaysOn;
                    }
                    else if (text == "switch")
                    {
                        Mode = ModeType.Switch;
                    }
                    else if (text == "pushbuttons")
                    {
                        Mode = ModeType.PushButtons;
                    }
                    else
                    {
                        STFException.TraceWarning(stf, "Skipped invalid battery switch mode");
                    }
                    break;

                case "engine(ortsbattery(delay":
                case "wagon(ortsbattery(delay":
                    DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0f);
                    break;

                case "engine(ortsbattery(defaulton":
                case "wagon(ortsbattery(defaulton":
                    DefaultOn = stf.ReadBoolBlock(false);
                    break;
            }
        }

        public void Copy(BatterySwitch other)
        {
            Mode = other.Mode;
            DelayS = other.DelayS;
            DefaultOn = other.DefaultOn;
        }

        public virtual void Initialize()
        {
            Timer.Setup(DelayS);

            if (DefaultOn)
            {
                On = true;

                if (Mode == ModeType.Switch)
                {
                    CommandSwitch = true;
                }
            }
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
            On = true;

            if (Mode == ModeType.Switch)
            {
                CommandSwitch = true;
            }
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(CommandSwitch);
            outf.Write(CommandButtonOn);
            outf.Write(CommandButtonOff);
            outf.Write(On);
        }

        public virtual void Restore(BinaryReader inf)
        {
            CommandSwitch = inf.ReadBoolean();
            CommandButtonOn = inf.ReadBoolean();
            CommandButtonOff = inf.ReadBoolean();
            On = inf.ReadBoolean();
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            switch (Mode)
            {
                case ModeType.AlwaysOn:
                    On = true;
                    break;

                case ModeType.Switch:
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
                                Wagon.SignalEvent(Event.BatterySwitchOff);
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
                            if (!Timer.Started)
                            {
                                Timer.Start();
                            }

                            if (Timer.Triggered)
                            {
                                On = true;
                                Wagon.SignalEvent(Event.BatterySwitchOn);
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
                    break;

                case ModeType.PushButtons:
                    if (On)
                    {
                        if (QuickPowerOn)
                        {
                            QuickPowerOn = false;
                            if (CommandButtonOn)
                            {
                                CommandButtonOn = false;
                                Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                            }
                        }
                        if (CommandButtonOff)
                        {
                            if (!Timer.Started)
                            {
                                Timer.Start();
                            }

                            if (Timer.Triggered)
                            {
                                On = false;
                                Wagon.SignalEvent(Event.BatterySwitchOff);
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
                        if (QuickPowerOff)
                        {
                            QuickPowerOff = false;
                            if (CommandButtonOff)
                            {
                                CommandButtonOff = false;
                                Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                            }
                        }
                        if (CommandButtonOn)
                        {
                            if (!Timer.Started)
                            {
                                Timer.Start();
                            }

                            if (Timer.Triggered)
                            {
                                On = true;
                                Wagon.SignalEvent(Event.BatterySwitchOn);
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
                    break;
            }
        }

        public virtual void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.CloseBatterySwitch:
                    if (Mode == ModeType.Switch && !CommandSwitch)
                    {
                        CommandSwitch = true;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                    }
                    break;

                case PowerSupplyEvent.OpenBatterySwitch:
                    if (Mode == ModeType.Switch && CommandSwitch)
                    {
                        CommandSwitch = false;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                    }
                    break;

                case PowerSupplyEvent.CloseBatterySwitchButtonPressed:
                    if (Mode == ModeType.PushButtons && !CommandButtonOn)
                    {
                        CommandButtonOn = true;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                    }
                    break;

                case PowerSupplyEvent.CloseBatterySwitchButtonReleased:
                    if (Mode == ModeType.PushButtons && CommandButtonOn)
                    {
                        CommandButtonOn = false;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                    }
                    break;

                case PowerSupplyEvent.OpenBatterySwitchButtonPressed:
                    if (Mode == ModeType.PushButtons && !CommandButtonOff)
                    {
                        CommandButtonOff = true;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                    }
                    break;

                case PowerSupplyEvent.OpenBatterySwitchButtonReleased:
                    if (Mode == ModeType.PushButtons && CommandButtonOff)
                    {
                        CommandButtonOff = false;
                        Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                    }
                    break;
                case PowerSupplyEvent.QuickPowerOn:
                    switch (Mode)
                    {
                        case ModeType.Switch:
                            CommandSwitch = true;
                            Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                            break;
                        case ModeType.PushButtons:
                            CommandButtonOn = true;
                            Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                            QuickPowerOn = true;
                            QuickPowerOff = false;
                            break;

                    }
                    break;
                case PowerSupplyEvent.QuickPowerOff:
                    switch (Mode)
                    {
                        case ModeType.Switch:
                            CommandSwitch = false;
                            Wagon.SignalEvent(Event.BatterySwitchCommandOff);
                            break;
                        case ModeType.PushButtons:
                            CommandButtonOff = true;
                            Wagon.SignalEvent(Event.BatterySwitchCommandOn);
                            QuickPowerOn = false;
                            QuickPowerOff = true;
                            break;
                    }
                    break;
            }
        }
    }
}
