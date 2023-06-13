// COPYRIGHT 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team.

using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Diagnostics;   // Used by Trace.Warnings
using System.IO;

namespace Orts.Common
{
    /// <summary>
    /// This Command Pattern allows requests to be encapsulated as objects (http://sourcemaking.com/design_patterns/command).
    /// The pattern provides many advantages, but it allows OR to record the commands and then to save them when the user presses F2.
    /// The commands can later be read from file and replayed.
    /// Writing and reading is done using the .NET binary serialization which is quick to code. (For an editable version, JSON has
    /// been successfully explored.)
    /// 
    /// Immediate commands (e.g. sound horn) are straightforward but continuous commands (e.g. apply train brake) are not. 
    /// OR aims for commands which can be repeated accurately and possibly on a range of hardware. Continuous commands therefore
    /// have a target value which is recorded once the key is released. OR creates an immediate command as soon as the user 
    /// presses the key, but OR creates the continuous command once the user releases the key and the target is known. 
    /// 
    /// All commands record the time when the command is created, but a continuous command backdates the time to when the key
    /// was pressed.
    /// 
    /// Each command class has a Receiver property and calls methods on the Receiver to execute the command.
    /// This property is static for 2 reasons:
    /// - so all command objects of the same class will share the same Receiver object;
    /// - so when a command is serialized to and deserialised from file, its Receiver does not have to be saved 
    ///   (which would be impractical) but is automatically available to commands which have been re-created from file.
    /// 
    /// Before each command class is used, this Receiver must be assigned, e.g.
    ///   ReverserCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
    /// 
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// The time when the command was issued (compatible with Simlator.ClockTime).
        /// </summary>
        double Time { get; set; }

        /// <summary>
        /// Call the Receiver to repeat the Command.
        /// Each class of command shares a single object, the Receiver, and the command executes by
        /// call methods of the Receiver.
        /// </summary>
        void Redo();

        /// <summary>
        /// Print the content of the command.
        /// </summary>
        void Report();
    }

    [Serializable()]
    public abstract class Command : ICommand
    {
        public double Time { get; set; }

        /// <summary>
        /// Each command adds itself to the log when it is constructed.
        /// </summary>
        public Command(CommandLog log)
        {
            log.CommandAdd(this as ICommand);
        }

        // Method required by ICommand
        public virtual void Redo() { Trace.TraceWarning("Dummy method"); }

        public override string ToString()
        {
            return this.GetType().ToString();
        }

        // Method required by ICommand
        public virtual void Report()
        {
            Trace.WriteLine(String.Format(
               "Command: {0} {1}", FormatStrings.FormatPreciseTime(Time), ToString()));
        }
    }

    // <Superclasses>
    [Serializable()]
    public abstract class BooleanCommand : Command
    {
        protected bool ToState;

        public BooleanCommand(CommandLog log, bool toState)
            : base(log)
        {
            ToState = toState;
        }
    }

    [Serializable()]
    public abstract class IndexCommand : Command
    {
        protected int Index;

        public IndexCommand(CommandLog log, int index)
            : base(log)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Superclass for continuous commands. Do not create a continuous command until the operation is complete.
    /// </summary>
    [Serializable()]
    public abstract class ContinuousCommand : BooleanCommand
    {
        protected float? Target;

        public ContinuousCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState)
        {
            Target = target;
            this.Time = startTime;   // Continuous commands are created at end of change, so overwrite time when command was created
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "increase" : "decrease") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public abstract class PausedCommand : Command
    {
        public double PauseDurationS;

        public PausedCommand(CommandLog log, double pauseDurationS)
            : base(log)
        {
            PauseDurationS = pauseDurationS;
        }

        public override string ToString()
        {
            return String.Format("{0} Paused Duration: {1}", base.ToString(), PauseDurationS);
        }
    }

    [Serializable()]
    public abstract class CameraCommand : Command
    {
        public CameraCommand(CommandLog log)
            : base(log)
        {
        }
    }

    [Serializable()]
    public sealed class SaveCommand : Command {
        public string FileStem;

        public SaveCommand( CommandLog log, string fileStem ) 
            : base( log ){
            this.FileStem = fileStem;
            Redo();
        }

        public override void Redo() {
            // Redo does nothing as SaveCommand is just a marker and saves the fileStem but is not used during replay to redo the save.
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " to file \"" + FileStem + ".replay\"";
        }
    }

    // Direction
    [Serializable()]
    public sealed class ReverserCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public ReverserCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            if( ToState ) {
                Receiver.StartReverseIncrease( null );
            } else {
                Receiver.StartReverseDecrease( null );
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "step forward" : "step back");
        }
    }

    [Serializable()]
    public sealed class ContinuousReverserCommand : ContinuousCommand {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousReverserCommand( CommandLog log, bool toState, float? target, double startTime ) 
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.ReverserChangeTo( ToState, Target );
            // Report();
        }
    }

    // Power : Raise/lower pantograph
    [Serializable()]
    public sealed class PantographCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }
        private int item;

        public PantographCommand( CommandLog log, int item, bool toState ) 
            : base( log, toState ) {
            this.item = item;
            Redo();
        }

        public override void Redo() {
            if (Receiver != null && Receiver.Train != null)
            {
                Receiver.Train.SignalEvent((ToState ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph), item);
            }
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "raise" : "lower") + ", item = " + item.ToString();
        }
    }

    // Power : Close/open circuit breaker switch
    [Serializable()]
    public sealed class CircuitBreakerClosingOrderCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public CircuitBreakerClosingOrderCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseCircuitBreaker : PowerSupplyEvent.OpenCircuitBreaker);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "close" : "open");
        }
    }

    // Power : Close circuit breaker button
    [Serializable()]
    public sealed class CircuitBreakerClosingOrderButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public CircuitBreakerClosingOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseCircuitBreakerButtonPressed : PowerSupplyEvent.CloseCircuitBreakerButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Open circuit breaker button
    [Serializable()]
    public sealed class CircuitBreakerOpeningOrderButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public CircuitBreakerOpeningOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.OpenCircuitBreakerButtonPressed : PowerSupplyEvent.OpenCircuitBreakerButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Give/remove circuit breaker authorization switch
    [Serializable()]
    public sealed class CircuitBreakerClosingAuthorizationCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public CircuitBreakerClosingAuthorizationCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.GiveCircuitBreakerClosingAuthorization : PowerSupplyEvent.RemoveCircuitBreakerClosingAuthorization);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "given" : "removed");
        }
    }

    // Power : Close/open traction cut-off relay switch
    [Serializable()]
    public sealed class TractionCutOffRelayClosingOrderCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public TractionCutOffRelayClosingOrderCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseTractionCutOffRelay : PowerSupplyEvent.OpenTractionCutOffRelay);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "close" : "open");
        }
    }

    // Power : Close traction cut-off relay button
    [Serializable()]
    public sealed class TractionCutOffRelayClosingOrderButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public TractionCutOffRelayClosingOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseTractionCutOffRelayButtonPressed : PowerSupplyEvent.CloseTractionCutOffRelayButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Open traction cut-off relay button
    [Serializable()]
    public sealed class TractionCutOffRelayOpeningOrderButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public TractionCutOffRelayOpeningOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.OpenTractionCutOffRelayButtonPressed : PowerSupplyEvent.OpenTractionCutOffRelayButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Give/remove traction cut-off relay closing authorization switch
    [Serializable()]
    public sealed class TractionCutOffRelayClosingAuthorizationCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public TractionCutOffRelayClosingAuthorizationCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.GiveTractionCutOffRelayClosingAuthorization : PowerSupplyEvent.RemoveTractionCutOffRelayClosingAuthorization);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "given" : "removed");
        }
    }

    // Power : Service retention button
    [Serializable()]
    public sealed class ServiceRetentionButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public ServiceRetentionButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.ServiceRetentionButtonPressed : PowerSupplyEvent.ServiceRetentionButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Service retention cancellation button
    [Serializable()]
    public sealed class ServiceRetentionCancellationButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public ServiceRetentionCancellationButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.ServiceRetentionCancellationButtonPressed : PowerSupplyEvent.ServiceRetentionCancellationButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power
    [Serializable()]
    public sealed class PowerCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public PowerCommand(CommandLog log, MSTSLocomotive receiver, bool toState)
            : base(log, toState)
        {
            Receiver = receiver;
            Redo();
        }

        public override void Redo()
        {
            Receiver?.SetPower(ToState);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    // MU commands connection
    [Serializable()]
    public sealed class ToggleMUCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleMUCommand(CommandLog log, MSTSLocomotive receiver, bool toState)
            : base(log, toState)
        {
            Receiver = receiver;
            Redo();
        }

        public override void Redo()
        {
            Receiver?.ToggleMUCommand(ToState);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    [Serializable()]
    public sealed class NotchedThrottleCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public NotchedThrottleCommand(CommandLog log, bool toState) : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AdjustNotchedThrottle(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "step forward" : "step back");
        }
    }

    [Serializable()]
    public sealed class ContinuousThrottleCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ContinuousThrottleCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ThrottleChangeTo(ToState, Target);
            // Report();
        }
    }

    // Brakes
    [Serializable()]
    public sealed class TrainBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TrainBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TrainBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class EngineBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EngineBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.EngineBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class BrakemanBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }
        public BrakemanBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakemanBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class DynamicBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public DynamicBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.DynamicBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class InitializeBrakesCommand : Command
    {
        public static Train Receiver { get; set; }

        public InitializeBrakesCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.UnconditionalInitializeBrakes();
        }
    }

    [Serializable()]
    public sealed class ResetOutOfControlModeCommand : Command
    {
        public static Train Receiver { get; set; }
        public ResetOutOfControlModeCommand(CommandLog log) : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualResetOutOfControlMode();
            Receiver.SignalEvent(TCSEvent.ManualResetOutOfControlMode);
        }
    }

    [Serializable()]
    public sealed class EmergencyPushButtonCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EmergencyPushButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.EmergencyButtonPressed = ToState;
            Receiver.TrainBrakeController.EmergencyBrakingPushButton = ToState;
        }
    }

    [Serializable()]
    public sealed class BailOffCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BailOffCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetBailOff(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "disengage" : "engage");
        }
    }

    [Serializable()]
    public sealed class QuickReleaseCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public QuickReleaseCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TrainBrakeController.QuickReleaseButtonPressed = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "off" : "on");
        }
    }

    [Serializable()]
    public sealed class BrakeOverchargeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BrakeOverchargeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TrainBrakeController.OverchargeButtonPressed = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "off" : "on");
        }
    }

    [Serializable()]
    public sealed class HandbrakeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HandbrakeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetTrainHandbrake(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class WagonHandbrakeCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public WagonHandbrakeCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetWagonHandbrake(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class RetainersCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public RetainersCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetTrainRetainers(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class BrakeHoseConnectCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BrakeHoseConnectCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeHoseConnect(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    [Serializable()]
    public sealed class WagonBrakeHoseConnectCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public WagonBrakeHoseConnectCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.FrontBrakeHoseConnected = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    [Serializable()]
    public sealed class ToggleAngleCockACommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleAngleCockACommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.AngleCockAOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class ToggleAngleCockBCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleAngleCockBCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.AngleCockBOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class ToggleBleedOffValveCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleBleedOffValveCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.BleedOffValveOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class SanderCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public SanderCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                if (!Receiver.Sander)
                    Receiver.Train.SignalEvent(Event.SanderOn);
            }
            else
            {
                Receiver.Train.SignalEvent(Event.SanderOff);
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "on" : "off");
        }
    }

    [Serializable()]
    public sealed class AlerterCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public AlerterCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState) Receiver.SignalEvent(Event.VigilanceAlarmReset); // There is no Event.VigilanceAlarmResetReleased
            Receiver.AlerterPressed(ToState);
            // Report();
        }
    }

    [Serializable()]
    public sealed class VacuumExhausterCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public VacuumExhausterCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                if (!Receiver.VacuumExhausterPressed)
                    Receiver.Train.SignalEvent(Event.VacuumExhausterOn);
            }
            else
            {
                Receiver.Train.SignalEvent(Event.VacuumExhausterOff);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "fast" : "normal");
        }
    }

    [Serializable()]
    public sealed class HornCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HornCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualHorn = ToState;
            if (ToState)
            {
                Receiver.AlerterReset(TCSEvent.HornActivated);
                Receiver.Simulator.HazzardManager.Horn();
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "sound" : "off");
        }
    }

    [Serializable()]
    public sealed class BellCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BellCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualBell = ToState;
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "ring" : "off");
        }
    }

    [Serializable()]
    public sealed class ToggleCabLightCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCabLightCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCabLight();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class HeadlightCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }
        public static bool MasterKeyHeadlightControl => Receiver.LocomotivePowerSupply.MasterKey.HeadlightControl;

        public HeadlightCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                switch (Receiver.Headlight)
                {
                    case 0:
                        if (!MasterKeyHeadlightControl)
                        {
                            Receiver.Headlight = 1;
                            Receiver.Simulator.Confirmer.Confirm(CabControl.Headlight, CabSetting.Neutral);
                            Receiver.SignalEvent(Event.LightSwitchToggle);
                        }
                        break;
                    case 1:
                        Receiver.Headlight = 2;
                        Receiver.Simulator.Confirmer.Confirm(CabControl.Headlight, CabSetting.On);
                        Receiver.SignalEvent(Event.LightSwitchToggle);
                        break;
                }
                
            }
            else
            {
                switch (Receiver.Headlight)
                {
                    case 1:
                        if (!MasterKeyHeadlightControl)
                        {
                            Receiver.Headlight = 0;
                            Receiver.Simulator.Confirmer.Confirm(CabControl.Headlight, CabSetting.Off);
                            Receiver.SignalEvent(Event.LightSwitchToggle);
                        }
                        break;
                    case 2:
                        Receiver.Headlight = 1;
                        Receiver.Simulator.Confirmer.Confirm(CabControl.Headlight, CabSetting.Neutral);
                        Receiver.SignalEvent(Event.LightSwitchToggle);
                        break;
                }
            }
        }
    }

    [Serializable()]
    public sealed class WipersCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public WipersCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleWipers(ToState);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleDoorsLeftCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsLeftCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            var side = Receiver.GetCabFlipped() ^ Receiver.Flipped ? DoorSide.Right : DoorSide.Left;
            var state = Receiver.Train.DoorState(side);
            Receiver.Train.SetDoors(side, state <= DoorState.Closing);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleDoorsRightCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsRightCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            var side = Receiver.GetCabFlipped() ^ Receiver.Flipped ? DoorSide.Left : DoorSide.Right;
            var state = Receiver.Train.DoorState(side);
            Receiver.Train.SetDoors(side, state <= DoorState.Closing);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleMirrorsCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleMirrorsCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleMirrors();
        }
    }

    [Serializable()]
    public sealed class ToggleBatterySwitchCommand : BooleanCommand
    {
        public static BatterySwitch Receiver { get; set; }

        public ToggleBatterySwitchCommand(CommandLog log, MSTSWagon wagon, bool toState)
            : base(log, toState)
        {
            Receiver = wagon?.PowerSupply?.BatterySwitch;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver?.Mode == BatterySwitch.ModeType.Switch)
            {
                Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseBatterySwitch : PowerSupplyEvent.OpenBatterySwitch);
            }
            else if (Receiver?.Mode == BatterySwitch.ModeType.PushButtons)
            {
                Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseBatterySwitchButtonPressed : PowerSupplyEvent.OpenBatterySwitchButtonPressed);
                Receiver?.Update(0f);
                Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseBatterySwitchButtonReleased : PowerSupplyEvent.OpenBatterySwitchButtonReleased);
            }
        }
    }

    [Serializable()]
    public sealed class BatterySwitchCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public BatterySwitchCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseBatterySwitch : PowerSupplyEvent.OpenBatterySwitch);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "close" : "open");
        }
    }

    [Serializable()]
    public sealed class BatterySwitchCloseButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public BatterySwitchCloseButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.CloseBatterySwitchButtonPressed : PowerSupplyEvent.CloseBatterySwitchButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    [Serializable()]
    public sealed class BatterySwitchOpenButtonCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public BatterySwitchOpenButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.OpenBatterySwitchButtonPressed : PowerSupplyEvent.OpenBatterySwitchButtonReleased);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    [Serializable()]
    public sealed class ToggleMasterKeyCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public ToggleMasterKeyCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.TurnOnMasterKey : PowerSupplyEvent.TurnOffMasterKey);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "turned on" : "turned off");
        }
    }

    [Serializable()]
    public sealed class ElectricTrainSupplyCommand : BooleanCommand
    {
        public static ILocomotivePowerSupply Receiver { get; set; }

        public ElectricTrainSupplyCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.HandleEvent(ToState ? PowerSupplyEvent.SwitchOnElectricTrainSupply : PowerSupplyEvent.SwitchOffElectricTrainSupply);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "switched on" : "switched off");
        }
    }

    [Serializable()]
    public sealed class ConnectElectricTrainSupplyCableCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ConnectElectricTrainSupplyCableCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.PowerSupply.FrontElectricTrainSupplyCableConnected = ToState;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    // Distributed Power controls
    [Serializable()]
    public sealed class DPMoveToFrontCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPMoveToFrontCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPMoveToFront();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPMoveToBackCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPMoveToBackCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPMoveToBack();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPIdleCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPIdleCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPIdle();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPTractionCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPTractionCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPTraction();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPDynamicBrakeCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPDynamicBrakeCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPDynamicBrake();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPMoreCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPMoreCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPMore();
            // Report();
        }
    }

    [Serializable()]
    public sealed class DPLessCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public DPLessCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.DPLess();
            // Report();
        }
    }


    // Steam controls
    // Steam booster command
    [Serializable()]
    public sealed class ContinuousSteamBoosterCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousSteamBoosterCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.SteamBoosterChangeTo(ToState, Target);
            }
            // Report();
        }
    }

    // Steam heat command
    [Serializable()]
    public sealed class ContinuousSteamHeatCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ContinuousSteamHeatCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.SteamHeatChangeTo(ToState, Target);
            }
            // Report();
        }
    }

    // Large Ejector command
    [Serializable()]
    public sealed class ContinuousLargeEjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousLargeEjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.LargeEjectorChangeTo(ToState, Target);
            }
            // Report();
        }
    }


    [Serializable()]
    public sealed class ContinuousSmallEjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousSmallEjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.SmallEjectorChangeTo(ToState, Target);
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousInjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }
        int Injector;

        public ContinuousInjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Injector = injector;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            switch (Injector)
            {
                case 1: { Receiver.Injector1ChangeTo(ToState, Target); break; }
                case 2: { Receiver.Injector2ChangeTo(ToState, Target); break; }
            }
            // Report();
        }

        public override string ToString()
        {
            return String.Format("Command: {0} {1} {2}", FormatStrings.FormatPreciseTime(Time), this.GetType().ToString(), Injector)
                + (ToState ? "open" : "close") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleInjectorCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }
        private int injector;

        public ToggleInjectorCommand(CommandLog log, int injector)
            : base(log)
        {
            this.injector = injector;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            switch (injector)
            {
                case 1: { Receiver.ToggleInjector1(); break; }
                case 2: { Receiver.ToggleInjector2(); break; }
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + injector.ToString();
        }
    }

    [Serializable()]
    public sealed class ContinuousBlowerCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousBlowerCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.BlowerChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousDamperCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousDamperCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.DamperChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousFireboxDoorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousFireboxDoorCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FireboxDoorChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousFiringRateCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousFiringRateCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FiringRateChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleManualFiringCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleManualFiringCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleManualFiring();
            // Report();
        }
    }

    [Serializable()]
    public sealed class AIFireOnCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireOnCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireOn();

        }

    }

    [Serializable()]
    public sealed class AIFireOffCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireOffCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireOff();

        }

    }

    [Serializable()]
    public sealed class AIFireResetCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireResetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireReset();

        }

    }

    [Serializable()]
    public sealed class FireShovelfullCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public FireShovelfullCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FireShovelfull();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleOdometerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleOdometerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerToggle();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ResetOdometerCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ResetOdometerCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerReset(ToState);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleOdometerDirectionCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleOdometerDirectionCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerToggleDirection();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleWaterScoopCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleWaterScoopCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleWaterScoop();
        }
    }

    // Cylinder Cocks command
    [Serializable()]
    public sealed class ToggleCylinderCocksCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleCylinderCocksCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleCylinderCocks();
            // Report();
        }
    }

    // Compound Valve command
    [Serializable()]
    public sealed class ToggleCylinderCompoundCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleCylinderCompoundCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleCylinderCompound();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleBlowdownValveCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleBlowdownValveCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleBlowdownValve();
            // Report();
        }
    }

    // Diesel player engine on / off command
    [Serializable()]
    public sealed class TogglePlayerEngineCommand : Command
    {
        public static MSTSDieselLocomotive Receiver { get; set; }

        public TogglePlayerEngineCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.LocomotivePowerSupply.HandleEvent(PowerSupplyEvent.TogglePlayerEngine);
        }
    }

    // Diesel helpers engine on / off command
    [Serializable()]
    public sealed class ToggleHelpersEngineCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHelpersEngineCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.LocomotivePowerSupply.HandleEvent(PowerSupplyEvent.ToggleHelperEngine);
        }
    }

    // Cab radio switch on-switch off command
    [Serializable()]
    public sealed class CabRadioCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public CabRadioCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null)
            {
                Receiver.ToggleCabRadio(ToState);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "switched on" : "switched off");
        }
    }

    [Serializable()]
    public sealed class TurntableClockwiseCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableClockwiseCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.StartContinuous(true);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Clockwise";
        }
    }


    [Serializable()]
    public sealed class TurntableClockwiseTargetCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableClockwiseTargetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ComputeTarget(true);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Clockwise with target";
        }
    }

    [Serializable()]
    public sealed class TurntableCounterclockwiseCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableCounterclockwiseCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.StartContinuous(false);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Counterclockwise";
        }
    }


    [Serializable()]
    public sealed class TurntableCounterclockwiseTargetCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableCounterclockwiseTargetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ComputeTarget(false);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Counterclockwise with target";
        }
    }

    [Serializable()]
    public sealed class ToggleGenericItem1Command : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleGenericItem1Command(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.GenericItem1Toggle();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleGenericItem2Command : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleGenericItem2Command(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.GenericItem2Toggle();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    // EOT Commands

    [Serializable()]
    public sealed class EOTCommTestCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EOTCommTestCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.Train?.EOT?.CommTest();
        }
    }

    [Serializable()]
    public sealed class EOTDisarmCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EOTDisarmCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.Train?.EOT?.Disarm();
        }
    }

    [Serializable()]
    public sealed class EOTArmTwoWayCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EOTArmTwoWayCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.Train?.EOT?.ArmTwoWay();
        }
    }

    [Serializable()]
    public sealed class EOTEmergencyBrakeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EOTEmergencyBrakeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver?.Train?.EOT?.EmergencyBrake(ToState);
        }
    }

    [Serializable()]
    public sealed class ToggleEOTEmergencyBrakeCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleEOTEmergencyBrakeCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.Train.EOT?.EmergencyBrake(!Receiver.Train.EOT.EOTEmergencyBrakingOn);
        }
    }

    [Serializable()]
    public sealed class EOTMountCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }
        public string PickedEOTType;

        public EOTMountCommand(CommandLog log, bool toState, string pickedEOTType)
            : base(log, toState)
        {
            PickedEOTType = pickedEOTType;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver?.Train != null)
            {
                var wagonFilePath = PickedEOTType.ToLower();
                if (ToState)
                {
                    try
                    {
                        EOT eot = (EOT)RollingStock.Load(Receiver.Train.Simulator, Receiver.Train, wagonFilePath);
                        eot.CarID = Receiver.Train.Number.ToString() + " - EOT";
                        eot.Train.EOT = eot;
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(wagonFilePath, error));
                        return;
                    }
                    Receiver.Train.CalculatePositionOfEOT();
                    Receiver.Train.physicsUpdate(0);
                }
                else
                {
                    var car = Receiver.Train.Cars[Receiver.Train.Cars.Count - 1];
                    if (wagonFilePath != car.WagFilePath.ToLower())
                    {
                        car = Receiver.Train.Cars[0];
                        if (Receiver.Train.LeadLocomotive != null) Receiver.Train.LeadLocomotiveIndex--;
                    }
                    else
                        Receiver.Train.RecalculateRearTDBTraveller();
                    car.Train = null;
                    car.IsPartOfActiveTrain = false;  // to stop sounds
                    Receiver.Train.Cars.Remove(car);
                    Receiver.Train.EOT = null;
                    Receiver.Train.physicsUpdate(0);
                }
            }
        }
    }

}
