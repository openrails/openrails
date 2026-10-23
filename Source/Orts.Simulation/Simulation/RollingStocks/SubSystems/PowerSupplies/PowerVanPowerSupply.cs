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
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class ScriptedPowerVanPowerSupply : ScriptedPowerSupply
    {
        public DieselEngines DieselEngines = null;

        protected bool Activated = false;
        protected string ScriptName = "Default";
        protected PowerVanPowerSupply Script => AbstractScript as PowerVanPowerSupply;

        // Variables
        public float PowerOnDelayS { get; protected set; } = 0f;

        public ScriptedPowerVanPowerSupply(MSTSWagon wagon) : base(wagon)
        {
            ElectricTrainSupplyDirection = PowerSupplyDirection.Source;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "wagon(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "wagon(ortsbattery":
                    Battery.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);
            if (other is ScriptedPowerVanPowerSupply scriptedOther)
            {
                ScriptName = scriptedOther.ScriptName;

                PowerOnDelayS = scriptedOther.PowerOnDelayS;
            }
        }

        public override void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Wagon.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as PowerVanPowerSupply;
                }
                if (Script == null)
                {
                    AbstractScript = new DefaultPowerVanPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }

            base.Initialize();
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);
            Script?.Update(elapsedClockSeconds);
        }
    }
    public class DefaultPowerVanPowerSupply : PowerVanPowerSupply
    {
        private Timer PowerOnTimer;

        private bool QuickPowerOn = false;

        public override void Initialize()
        {
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());
        }

        public override void InitializeMoving()
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() || CurrentElectricTrainSupplyState() == PowerSupplyState.PowerOn ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            if (QuickPowerOn)
            {
                if (CurrentPantographState() == PantographState.Up)
                {
                    SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOn);
                    QuickPowerOn = false;
                }
                if (CurrentDieselEnginesState() == DieselEngineState.Running)
                {
                    QuickPowerOn = false;
                }
            }

            if ((CurrentPantographState() == PantographState.Up && CurrentCircuitBreakerState() == CircuitBreakerState.Closed)
                || CurrentDieselEnginesState() == DieselEngineState.Running)
            {
                if (!PowerOnTimer.Started)
                {
                    PowerOnTimer.Start();
                    SignalEvent(Event.PowerConverterOn);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOnOngoing);
                }

                if (PowerOnTimer.Triggered && CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOn)
                {
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                }
            }
            else
            {
                if (PowerOnTimer.Started)
                    PowerOnTimer.Stop();
                if (CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOff)
                {
                    SignalEvent(Event.PowerConverterOff);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    PowerOnTimer.Stop();
                }
            }
            SetCurrentElectricTrainSupplyState(CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
        }
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToDieselEngines(PowerSupplyEvent.StartEngine);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOff);
                    SignalEventToPantographs(PowerSupplyEvent.LowerPantograph);
                    SignalEventToDieselEngines(PowerSupplyEvent.StopEngine);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
