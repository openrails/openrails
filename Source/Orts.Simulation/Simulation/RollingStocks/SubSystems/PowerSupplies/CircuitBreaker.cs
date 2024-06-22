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

using System;
using System.IO;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedCircuitBreaker : ITractionCutOffSubsystem
    {
        public ILocomotivePowerSupply PowerSupply { get; protected set; }
        public ScriptedLocomotivePowerSupply LocomotivePowerSupply => PowerSupply as ScriptedLocomotivePowerSupply;
        public MSTSLocomotive Locomotive => LocomotivePowerSupply.Locomotive;
        public Simulator Simulator => Locomotive.Simulator;

        public bool Activated = false;
        public string ScriptName { get; protected set; } = "Automatic";
        CircuitBreaker Script;

        public float DelayS { get; protected set; } = 0f;

        public CircuitBreakerState State { get; set; } = CircuitBreakerState.Open;
        public bool DriverClosingOrder { get; set; }
        public bool DriverOpeningOrder { get; set; }
        public bool DriverClosingAuthorization { get; set; }
        public bool TCSClosingOrder
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.CircuitBreakerClosingOrder;
                else
                    return false;
            }
        }
        public bool TCSOpeningOrder
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.CircuitBreakerOpeningOrder;
                else
                    return false;
            }
        }
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

        public ScriptedCircuitBreaker(ScriptedLocomotivePowerSupply powerSupply)
        {
            PowerSupply = powerSupply;
        }

        public void Copy(ITractionCutOffSubsystem other)
        {
            if (other is ScriptedCircuitBreaker cbOther)
            {
                ScriptName = cbOther.ScriptName;
                State = cbOther.State;
                DelayS = cbOther.DelayS;
            }
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortscircuitbreakerclosingdelay":
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
                    switch(ScriptName.ToLowerInvariant())
                    {
                        case "automatic":
                            Script = new AutomaticCircuitBreaker();
                            break;

                        case "manual":
                            Script = new ManualCircuitBreaker();
                            break;

                        case "pushbuttons":
                            Script = new PushButtonsCircuitBreaker();
                            break;

                        case "twostage":
                            Script = new TwoStageCircuitBreaker();
                            break;

                        default:
                            var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                            Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as CircuitBreaker;
                            break;
                    }
                }
                // Fallback to automatic circuit breaker if the above failed.
                if (Script == null)
                {
                    Script = new AutomaticCircuitBreaker();
                }

                Script.AttachToHost(this);

                // AbstractScriptClass
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.GameTime = () => (float)Simulator.GameTime;
                Script.PreUpdate = () => Simulator.PreUpdate;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
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

            State = CircuitBreakerState.Closed;
        }

        public void Update(float elapsedSeconds)
        {
            if (Locomotive.Train.TrainType == Train.TRAINTYPE.AI || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE
                || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || Locomotive.Train.Autopilot)
            {
                State = CircuitBreakerState.Closed;
            }
            else
            {
                if (Script != null)
                {
                    Script.Update(elapsedSeconds);
                }
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Script?.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            Script?.HandleEvent(evt, id);
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
            State = (CircuitBreakerState)Enum.Parse(typeof(CircuitBreakerState), inf.ReadString());
            DriverClosingOrder = inf.ReadBoolean();
            DriverOpeningOrder = inf.ReadBoolean();
            DriverClosingAuthorization = inf.ReadBoolean();
            ClosingAuthorization = inf.ReadBoolean();
        }
    }

    class AutomaticCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

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
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
                        SignalEvent(Event.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            // Nothing to do since it is automatic
        }
    }

    class ManualCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization() || DriverOpeningOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
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
                case PowerSupplyEvent.QuickPowerOn:
                case PowerSupplyEvent.CloseCircuitBreaker:
                    SetDriverClosingOrder(true);
                    SetDriverOpeningOrder(false);
                    SignalEvent(Event.CircuitBreakerClosingOrderOn);

                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.On);
                    if (!ClosingAuthorization())
                    {
                        Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Circuit breaker closing not authorized"));
                    }
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                case PowerSupplyEvent.OpenCircuitBreaker:
                    SetDriverClosingOrder(false);
                    SetDriverOpeningOrder(true);
                    SignalEvent(Event.CircuitBreakerClosingOrderOff);

                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.Off);
                    break;
            }
        }
    }

    public class PushButtonsCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;
        private bool QuickPowerOn;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization() || DriverOpeningOrder() || TCSOpeningOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization() && (DriverClosingOrder() || TCSClosingOrder() || QuickPowerOn))
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            QuickPowerOn = false;
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        QuickPowerOn = false;
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization() && (DriverClosingOrder() || TCSClosingOrder() || QuickPowerOn))
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    else if (QuickPowerOn)
                    {
                        QuickPowerOn = false;
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
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
                case PowerSupplyEvent.CloseCircuitBreakerButtonPressed:
                    SetDriverOpeningOrder(false);
                    SetDriverClosingOrder(true);
                    SignalEvent(Event.CircuitBreakerClosingOrderOn);
                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.On);
                    if (!ClosingAuthorization())
                    {
                        Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Circuit breaker closing not authorized"));
                    }
                    break;
                case PowerSupplyEvent.CloseCircuitBreakerButtonReleased:
                    SetDriverClosingOrder(false);
                    break;
                case PowerSupplyEvent.OpenCircuitBreakerButtonPressed:
                    SetDriverClosingOrder(false);
                    SetDriverOpeningOrder(true);
                    SignalEvent(Event.CircuitBreakerClosingOrderOff);
                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.Off);
                    break;
                case PowerSupplyEvent.OpenCircuitBreakerButtonReleased:
                    SetDriverOpeningOrder(false);
                    break;
                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    break;
                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SetCurrentState(CircuitBreakerState.Open);
                    break;
            }
        }
    }

    public class TwoStageCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        private Timer InitTimer;
        private bool QuickPowerOn;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());
        }

        public override void Update(float elapsedSeconds)
        {

            SetClosingAuthorization(TCSClosingAuthorization() && DriverClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization() || TCSOpeningOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization() && (DriverClosingOrder() || TCSClosingOrder() || QuickPowerOn))
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            QuickPowerOn = false;
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        QuickPowerOn = false;
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization() && (DriverClosingOrder() || TCSClosingOrder() || QuickPowerOn))
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    else if (QuickPowerOn)
                    {
                        QuickPowerOn = false;
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
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
                case PowerSupplyEvent.CloseCircuitBreakerButtonPressed:
                    SetDriverClosingOrder(true);
                    SignalEvent(Event.CircuitBreakerClosingOrderOn);
                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.On);
                    if (!ClosingAuthorization())
                    {
                        Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Circuit breaker closing not authorized"));
                    }
                    break;

                case PowerSupplyEvent.CloseCircuitBreakerButtonReleased:
                    SetDriverClosingOrder(false);
                    SignalEvent(Event.CircuitBreakerClosingOrderOff);
                    break;

                case PowerSupplyEvent.GiveCircuitBreakerClosingAuthorization:
                    SetDriverClosingAuthorization(true);
                    SignalEvent(Event.CircuitBreakerClosingAuthorizationOn);
                    Confirm(CabControl.CircuitBreakerClosingAuthorization, CabSetting.On);
                    break;

                case PowerSupplyEvent.RemoveCircuitBreakerClosingAuthorization:
                    SetDriverClosingAuthorization(false);
                    SignalEvent(Event.CircuitBreakerClosingAuthorizationOff);
                    Confirm(CabControl.CircuitBreakerClosingAuthorization, CabSetting.Off);
                    break;

                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    SetDriverClosingAuthorization(true);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SetDriverClosingAuthorization(false);
                    SetCurrentState(CircuitBreakerState.Open);
                    break;
            }
        }
    }
}
