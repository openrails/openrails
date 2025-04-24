﻿// COPYRIGHT 2020 by the Open Rails project.
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
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedTractionCutOffRelay : ITractionCutOffSubsystem
    {
        public ILocomotivePowerSupply PowerSupply { get; protected set; }
        public ScriptedLocomotivePowerSupply LocomotivePowerSupply => PowerSupply as ScriptedLocomotivePowerSupply;
        public MSTSLocomotive Locomotive => LocomotivePowerSupply.Locomotive;
        public Simulator Simulator => LocomotivePowerSupply.Locomotive.Simulator;

        public bool Activated = false;
        public string ScriptName { get; protected set; } = "Automatic";
        TractionCutOffRelay Script;

        public float DelayS { get; protected set; } = 0f;

        public TractionCutOffRelayState State { get; set; } = TractionCutOffRelayState.Open;
        public bool DriverClosingOrder { get; set; }
        public bool DriverOpeningOrder { get; set; }
        public bool DriverClosingAuthorization { get; set; }
        public bool TCSClosingAuthorization
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.PowerAuthorization;
                else
                    return false;
            }
        }
        public bool ClosingAuthorization { get; set; }

        public ScriptedTractionCutOffRelay(ScriptedLocomotivePowerSupply powerSupply)
        {
            PowerSupply = powerSupply;
        }

        public void Copy(ITractionCutOffSubsystem other)
        {
            if (other is ScriptedTractionCutOffRelay tcorOther)
            {
                ScriptName = tcorOther.ScriptName;
                State = tcorOther.State;
                DelayS = tcorOther.DelayS;
            }
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortstractioncutoffrelay":
                    if (Locomotive.Train as AITrain == null)
                    {
                        ScriptName = stf.ReadStringBlock(null);
                    }
                    break;

                case "engine(ortstractioncutoffrelayclosingdelay":
                    DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;
            }
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null)
                {
                    switch(ScriptName)
                    {
                        case "Automatic":
                            Script = new AutomaticTractionCutOffRelay() as TractionCutOffRelay;
                            break;

                        case "Manual":
                            Script = new ManualTractionCutOffRelay() as TractionCutOffRelay;
                            break;

                        default:
                            var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                            Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as TractionCutOffRelay;
                            break;
                    }
                }
                // Fallback to automatic circuit breaker if the above failed.
                if (Script == null)
                {
                    Script = new AutomaticTractionCutOffRelay() as TractionCutOffRelay;
                }

                Script.AttachToHost(this);

                // AbstractScriptClass
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.GameTime = () => (float)Simulator.GameTime;
                Script.PreUpdate = () => Simulator.PreUpdate;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.Confirm = Locomotive.Simulator.Confirmer.Confirm;
                Script.Message = Locomotive.Simulator.Confirmer.Message;
                Script.SignalEvent = Locomotive.SignalEvent;
                Script.SignalEventToTrain = (evt) =>
                {
                    if (Locomotive.Train != null)
                    {
                        Locomotive.Train.SignalEvent(evt);
                    }
                };

                Script.Initialize();
                Activated = true;
            }
        }

        public void InitializeMoving()
        {
            Script?.InitializeMoving();

            State = TractionCutOffRelayState.Closed;
        }

        public void Update(float elapsedSeconds)
        {
            if (Locomotive.Train.TrainType == Train.TRAINTYPE.AI || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE
                || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                State = TractionCutOffRelayState.Closed;
            }
            else
            {
                Script?.Update(elapsedSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Script?.HandleEvent(evt);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(DelayS);
            outf.Write(State.ToString());
            outf.Write(DriverClosingOrder);
            outf.Write(DriverOpeningOrder);
            outf.Write(DriverClosingAuthorization);
            outf.Write(ClosingAuthorization);
        }

        public void Restore(BinaryReader inf)
        {
            DelayS = inf.ReadSingle();
            State = (TractionCutOffRelayState)Enum.Parse(typeof(TractionCutOffRelayState), inf.ReadString());
            DriverClosingOrder = inf.ReadBoolean();
            DriverOpeningOrder = inf.ReadBoolean();
            DriverClosingAuthorization = inf.ReadBoolean();
            ClosingAuthorization = inf.ReadBoolean();
        }
    }

    class AutomaticTractionCutOffRelay : TractionCutOffRelay
    {
        private Timer ClosingTimer;
        private TractionCutOffRelayState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingOrder(false);
            SetDriverOpeningOrder(false);
            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds)
        {
            UpdateClosingAuthorization();

            switch (CurrentState())
            {
                case TractionCutOffRelayState.Closed:
                    if (!ClosingAuthorization())
                    {
                        SetCurrentState(TractionCutOffRelayState.Open);
                    }
                    break;

                case TractionCutOffRelayState.Closing:
                    if (ClosingAuthorization())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(TractionCutOffRelayState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(TractionCutOffRelayState.Open);
                    }
                    break;

                case TractionCutOffRelayState.Open:
                    if (ClosingAuthorization())
                    {
                        SetCurrentState(TractionCutOffRelayState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case TractionCutOffRelayState.Open:
                        SignalEvent(Event.TractionCutOffRelayOpen);
                        break;

                    case TractionCutOffRelayState.Closing:
                        SignalEvent(Event.TractionCutOffRelayClosing);
                        break;

                    case TractionCutOffRelayState.Closed:
                        SignalEvent(Event.TractionCutOffRelayClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public virtual void UpdateClosingAuthorization()
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentDieselEngineState() == DieselEngineState.Running);
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            // Nothing to do since it is automatic
        }
    }

    class AutomaticDualModeTractionCutOffRelay : AutomaticTractionCutOffRelay
    {
        public override void UpdateClosingAuthorization()
        {
            SetClosingAuthorization(
                TCSClosingAuthorization()
                && (
                    (CurrentPantographState() == PantographState.Up && CurrentCircuitBreakerState() == CircuitBreakerState.Closed)
                    || CurrentDieselEngineState() == DieselEngineState.Running
                )
            );
        }
    }

    class ManualTractionCutOffRelay : TractionCutOffRelay
    {
        private Timer ClosingTimer;
        private TractionCutOffRelayState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentDieselEngineState() == DieselEngineState.Running);

            switch (CurrentState())
            {
                case TractionCutOffRelayState.Closed:
                    if (!ClosingAuthorization() || DriverOpeningOrder())
                    {
                        SetCurrentState(TractionCutOffRelayState.Open);
                    }
                    break;

                case TractionCutOffRelayState.Closing:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(TractionCutOffRelayState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(TractionCutOffRelayState.Open);
                    }
                    break;

                case TractionCutOffRelayState.Open:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        SetCurrentState(TractionCutOffRelayState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case TractionCutOffRelayState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case TractionCutOffRelayState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case TractionCutOffRelayState.Closed:
                        SignalEvent(Event.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.CloseTractionCutOffRelay:
                    if (!DriverClosingOrder())
                    {
                        SetDriverClosingOrder(true);
                        SetDriverOpeningOrder(false);
                        SignalEvent(Event.TractionCutOffRelayClosingOrderOn);

                        Confirm(CabControl.TractionCutOffRelayClosingOrder, CabSetting.On);
                        if (!ClosingAuthorization())
                        {
                            Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Traction cut-off relay closing not authorized"));
                        }
                    }
                    break;

                case PowerSupplyEvent.OpenTractionCutOffRelay:
                    SetDriverClosingOrder(false);
                    SetDriverOpeningOrder(true);
                    SignalEvent(Event.TractionCutOffRelayClosingOrderOff);

                    Confirm(CabControl.TractionCutOffRelayClosingOrder, CabSetting.Off);
                    break;
            }
        }
    }
}
