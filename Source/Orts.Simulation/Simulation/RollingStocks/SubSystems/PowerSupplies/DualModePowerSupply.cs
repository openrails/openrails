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

using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedDualModePowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSElectricLocomotive DualModeLocomotive => Locomotive as MSTSElectricLocomotive;
        public Pantographs Pantographs => Locomotive.Pantographs;
        public ScriptedCircuitBreaker CircuitBreaker { get; protected set; }
        public ScriptedTractionCutOffRelay TractionCutOffRelay { get; protected set; }

        public override PowerSupplyType Type => PowerSupplyType.Electric;
        public bool Activated = false;
        private DualModePowerSupply Script => AbstractScript as DualModePowerSupply;

        public float LineVoltageV => (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
        public float PantographVoltageV { get; set; }
        public float FilterVoltageV { get; set; } = 0;

        public ScriptedDualModePowerSupply(MSTSElectricLocomotive locomotive) :
            base(locomotive)
        {
            CircuitBreaker = new ScriptedCircuitBreaker(this);
            TractionCutOffRelay = new ScriptedTractionCutOffRelay(this);
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortstractioncutoffrelay":
                case "engine(ortstractioncutoffrelayclosingdelay":
                    TractionCutOffRelay.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);

            if (other is ScriptedDualModePowerSupply scriptedOther)
            {
                CircuitBreaker.Copy(scriptedOther.CircuitBreaker);
                TractionCutOffRelay.Copy(scriptedOther.TractionCutOffRelay);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as DualModePowerSupply;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Script == null)
                {
                    AbstractScript = new DefaultDualModePowerSupply();
                }

                AssignScriptFunctions();

                Script.Initialize();
                Activated = true;
            }

            CircuitBreaker.Initialize();
            TractionCutOffRelay.Initialize();
        }


        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            CircuitBreaker.InitializeMoving();
            TractionCutOffRelay.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);

            base.Save(outf);
            CircuitBreaker.Save(outf);
            TractionCutOffRelay.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();

            base.Restore(inf);
            CircuitBreaker.Restore(inf);
            TractionCutOffRelay.Restore(inf);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            CircuitBreaker.Update(elapsedClockSeconds);
            TractionCutOffRelay.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }

        protected override void AssignScriptFunctions()
        {
            base.AssignScriptFunctions();

            // DualModePowerSupply getters
            Script.CurrentDieselEngineState = () => (Locomotive as MSTSDieselLocomotive).DieselEngines.State;
            Script.CurrentTractionCutOffRelayState = () => TractionCutOffRelay.State;
            Script.TractionCutOffRelayDriverClosingOrder = () => TractionCutOffRelay.DriverClosingOrder;
            Script.TractionCutOffRelayDriverOpeningOrder = () => TractionCutOffRelay.DriverOpeningOrder;
            Script.TractionCutOffRelayDriverClosingAuthorization = () => TractionCutOffRelay.DriverClosingAuthorization;

            // DualModePowerSupply setters
            Script.SignalEventToTractionCutOffRelay = (evt) => TractionCutOffRelay.HandleEvent(evt);
        }
    }

    public class DefaultDualModePowerSupply : DualModePowerSupply
    {
        private IIRFilter PantographFilter;
        private IIRFilter VoltageFilter;
        private Timer PowerOnTimer;
        private Timer AuxPowerOnTimer;

        public override void Initialize()
        {
            PantographFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            VoltageFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentPantographState())
            {
                case PantographState.Down:
                case PantographState.Lowering:
                case PantographState.Raising:
                    if (PowerOnTimer.Started)
                        PowerOnTimer.Stop();
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();

                    SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    SetPantographVoltageV(PantographFilter.Filter(0.0f, elapsedClockSeconds));
                    SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                    break;

                case PantographState.Up:
                    SetPantographVoltageV(PantographFilter.Filter(LineVoltageV(), elapsedClockSeconds));

                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Open:
                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();
                            if (AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Stop();

                            SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                            break;

                        case CircuitBreakerState.Closed:
                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();
                            if (!AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Start();

                            SetCurrentMainPowerSupplyState(PowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryPowerSupplyState(AuxPowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(PantographVoltageV(), elapsedClockSeconds));
                            break;
                    }
                    break;
            }
        }
    }
}
