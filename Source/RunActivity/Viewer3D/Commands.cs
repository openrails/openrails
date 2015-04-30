// COPYRIGHT 2012, 2013 by the Open Rails project.
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

using System;
using System.Diagnostics;   // Used by Trace.Warnings
using ORTS.Scripting.Api;
using ORTS.Viewer3D.Popups;
using ORTS.Viewer3D.RollingStock;

namespace ORTS.Viewer3D
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
    public interface ICommand {

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
    public abstract class Command : ICommand {
        public virtual double Time { get; set; }

        /// <summary>
        /// Each command adds itself to the log when it is constructed.
        /// </summary>
        public Command( CommandLog log ) {
            log.CommandAdd( this as ICommand );
        }

        // Method required by ICommand
        public virtual void Redo() { Trace.TraceWarning( "Dummy method" ); }

        public override string ToString() {
            return this.GetType().ToString();
        }

        // Method required by ICommand
        public virtual void Report() {
            Trace.WriteLine( String.Format(
               "Command: {0} {1}", InfoDisplay.FormattedPreciseTime( Time ), ToString() ) );
        }
    }

    // <Superclasses>
    [Serializable()]
    public abstract class BooleanCommand : Command {
        protected bool ToState;

        public BooleanCommand( CommandLog log, bool toState )
            : base( log ) {
            ToState = toState;
        }
    }

    [Serializable()]
    public abstract class IndexCommand : Command {
        protected int Index;

        public IndexCommand( CommandLog log, int index )
            : base(log)
        {
            Index = index;
        }
    }
    
    /// <summary>
    /// Superclass for continuous commands. Do not create a continuous command until the operation is complete.
    /// </summary>
    [Serializable()]
    public abstract class ContinuousCommand : BooleanCommand {
        protected float? Target;

        public ContinuousCommand( CommandLog log, bool toState, float? target, double startTime ) 
            : base( log, toState ) {
            Target = target;
            this.Time = startTime;   // Continuous commands are created at end of change, so overwrite time when command was created
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "increase" : "decrease") + ", target = " + Target.ToString();
        }
    }
    
    [Serializable()]
    public abstract class ActivityCommand : Command {
        public static ActivityWindow Receiver { get; set; }
        string EventNameLabel;
        public double PauseDurationS;

        public ActivityCommand( CommandLog log, string eventNameLabel, double pauseDurationS )
            : base( log ) {
            EventNameLabel = eventNameLabel;
            PauseDurationS = pauseDurationS;
            //Redo(); // More consistent but untested
        }

        public override string ToString() {
            return String.Format( "{0} Event: {1} ActionDuration: {2}", base.ToString(), EventNameLabel, PauseDurationS );
        }
    } // </Superclasses>

    [Serializable()]
    public class SaveCommand : Command {
        // <CJComment> Receiver is static so that all commands of this type will share it, 
        // especially new commands created by the deserializing process.
        public static Viewer Receiver { get; set; }
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
    public class ReverserCommand : BooleanCommand {
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
    public class ContinuousReverserCommand : ContinuousCommand {
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

    // Power
    [Serializable()]
    public class PantographCommand : BooleanCommand {
        public static MSTSElectricLocomotive Receiver { get; set; }
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

    // Power
    [Serializable()]
    public class PowerCommand : BooleanCommand
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
            if (Receiver == null) return;//no receiver of this panto
            Receiver.SetPower(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    // MU commands connection
    [Serializable()]
    public class ToggleMUCommand : BooleanCommand
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
            if (Receiver == null) return;//no receiver of this panto
            Receiver.ToggleMUCommand(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    [Serializable()]
    public class NotchedThrottleCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public NotchedThrottleCommand( CommandLog log, bool toState ) : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.AdjustNotchedThrottle(ToState);
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "step forward" : "step back");
        }
    }

    [Serializable()]
    public class ContinuousThrottleCommand : ContinuousCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public ContinuousThrottleCommand( CommandLog log, bool toState, float? target, double startTime ) 
            : base( log, toState, target, startTime ){
            Redo();
        }

        public override void Redo() { 
            Receiver.ThrottleChangeTo( ToState, Target );
            // Report();
        }
    }
    
    // Brakes
    [Serializable()]
    public class TrainBrakeCommand : ContinuousCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public TrainBrakeCommand( CommandLog log, bool toState, float? target, double startTime ) 
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            Receiver.TrainBrakeChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class EngineBrakeCommand : ContinuousCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public EngineBrakeCommand( CommandLog log, bool toState, float? target, double startTime )
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            Receiver.EngineBrakeChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class DynamicBrakeCommand : ContinuousCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public DynamicBrakeCommand( CommandLog log, bool toState, float? target, double startTime )
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            Receiver.DynamicBrakeChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class InitializeBrakesCommand : Command {
        public static Train Receiver { get; set; }

        public InitializeBrakesCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.UnconditionalInitializeBrakes();
            // Report();
        }
    }

    [Serializable()]
    public class EmergencyPushButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EmergencyPushButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.EmergencyButtonPressed = !Receiver.EmergencyButtonPressed;
            Receiver.TrainBrakeController.EmergencyBrakingPushButton = Receiver.EmergencyButtonPressed;
            // Report();
        }
    }

    [Serializable()]
    public class BailOffCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public BailOffCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.SetBailOff( ToState );
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "disengage" : "engage");
        }
    }

    [Serializable()]
    public class HandbrakeCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public HandbrakeCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.SetTrainHandbrake( ToState );
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public class WagonHandbrakeCommand : BooleanCommand
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
    public class RetainersCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public RetainersCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.SetTrainRetainers( ToState );
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public class BrakeHoseConnectCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public BrakeHoseConnectCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.BrakeHoseConnect( ToState );
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    [Serializable()]
    public class WagonBrakeHoseConnectCommand : BooleanCommand
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
    public class ToggleAngleCockACommand : BooleanCommand
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
    public class ToggleAngleCockBCommand : BooleanCommand
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
    public class ToggleBleedOffValveCommand : BooleanCommand
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
    public class SanderCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public SanderCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            if( ToState ) {
                if (!Receiver.Sander)
                    Receiver.Train.SignalEvent(Event.SanderOn);
            } else {
                Receiver.Train.SignalEvent(Event.SanderOff);
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + (ToState ? "on" : "off");
        }
    }

    [Serializable()]
    public class AlerterCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public AlerterCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            if (ToState) Receiver.SignalEvent(Event.VigilanceAlarmReset); // There is no Event.VigilanceAlarmResetReleased
            Receiver.AlerterPressed(ToState);
            // Report();
        }
    }

    [Serializable()]
    public class HornCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public HornCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            Receiver.SignalEvent(ToState ? Event.HornOn : Event.HornOff);
            if (ToState)
            {
                Receiver.AlerterReset(TCSEvent.HornActivated);
                Receiver.Simulator.HazzardManager.Horn();
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " " + (ToState ? "sound" : "off");
        }
    }

    [Serializable()]
    public class BellCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public BellCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            if (ToState)
            {
                if (!Receiver.Bell)
                    Receiver.SignalEvent(Event.BellOn);
            }
            else
            {
                Receiver.SignalEvent(Event.BellOff);
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " " + (ToState ? "ring" : "off");
        }
    }

    [Serializable()]
    public class ToggleCabLightCommand : Command {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCabLightCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ToggleCabLight( );
            // Report();
        }

        public override string ToString() {
            return base.ToString();
        }
    }

    [Serializable()]
    public class HeadlightCommand : BooleanCommand {
        public static MSTSLocomotive Receiver { get; set; }

        public HeadlightCommand( CommandLog log, bool toState ) 
            : base( log, toState ) {
            Redo();
        }

        public override void Redo() {
            if( ToState ) {
                switch( Receiver.Headlight ) {
                    case 0: Receiver.Headlight = 1; Receiver.Simulator.Confirmer.Confirm( CabControl.Headlight, CabSetting.Neutral ); break;
                    case 1: Receiver.Headlight = 2; Receiver.Simulator.Confirmer.Confirm( CabControl.Headlight, CabSetting.On ); break;
                }
                Receiver.SignalEvent(Event.LightSwitchToggle);
            } else {
                switch( Receiver.Headlight ) {
                    case 1: Receiver.Headlight = 0; Receiver.Simulator.Confirmer.Confirm( CabControl.Headlight, CabSetting.Off ); break;
                    case 2: Receiver.Headlight = 1; Receiver.Simulator.Confirmer.Confirm( CabControl.Headlight, CabSetting.Neutral ); break;
                }
                Receiver.SignalEvent(Event.LightSwitchToggle);
            }
            // Report();
        }
    }

    [Serializable()]
    public class ToggleWipersCommand : Command {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleWipersCommand( CommandLog log ) 
            : base( log ) { 
            Redo(); 
        }

        public override void Redo() {
            Receiver.ToggleWipers();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleDoorsLeftCommand : Command {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsLeftCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver.Flipped ^ Receiver.GetCabFlipped())  Receiver.ToggleDoorsRight();
            else Receiver.ToggleDoorsLeft();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleDoorsRightCommand : Command {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsRightCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver.Flipped ^ Receiver.GetCabFlipped()) Receiver.ToggleDoorsLeft();
            else Receiver.ToggleDoorsRight();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleMirrorsCommand : Command {
        public static MSTSWagon Receiver { get; set; }

        public ToggleMirrorsCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ToggleMirrors();
            // Report();
        }
    }
    
    // Steam controls
    [Serializable()]
    public class ContinuousInjectorCommand : ContinuousCommand {
        public static MSTSSteamLocomotive Receiver { get; set; }
        int Injector;

        public ContinuousInjectorCommand( CommandLog log, int injector, bool toState, float? target, double startTime ) 
            : base( log, toState, target, startTime ) {
            Injector = injector;
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            switch( Injector ) {
                case 1: { Receiver.Injector1ChangeTo( ToState, Target ); break; }
                case 2: { Receiver.Injector2ChangeTo( ToState, Target ); break; }
            }
            // Report();
        }

        public override string ToString() {
            return String.Format( "Command: {0} {1} {2}", InfoDisplay.FormattedPreciseTime( Time ), this.GetType().ToString(), Injector) 
                + (ToState ? "open" : "close") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public class ToggleInjectorCommand : Command {
        public static MSTSSteamLocomotive Receiver { get; set; }
        private int injector;

        public ToggleInjectorCommand( CommandLog log, int injector ) 
            : base( log ) {
            this.injector = injector;
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            switch( injector ) {
                case 1: { Receiver.ToggleInjector1(); break; }
                case 2: { Receiver.ToggleInjector2(); break; }
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + injector.ToString();
        }
    }

    [Serializable()]
    public class ContinuousBlowerCommand : ContinuousCommand {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousBlowerCommand( CommandLog log, bool toState, float? target, double startTime ) 
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.BlowerChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class ContinuousDamperCommand : ContinuousCommand {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousDamperCommand( CommandLog log, bool toState, float? target, double startTime )
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.DamperChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class ContinuousFireboxDoorCommand : ContinuousCommand
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
    public class ContinuousFiringRateCommand : ContinuousCommand {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousFiringRateCommand( CommandLog log, bool toState, float? target, double startTime )
            : base( log, toState, target, startTime ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.FiringRateChangeTo( ToState, Target );
            // Report();
        }
    }

    [Serializable()]
    public class ToggleManualFiringCommand : Command {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleManualFiringCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.ToggleManualFiring();
            // Report();
        }
    }

    [Serializable()]
    public class FireShovelfullCommand : Command {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public FireShovelfullCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.FireShovelfull();
            // Report();
        }
    }

    /// <summary>
    /// Continuous command to re-fuel and re-water locomotive or tender.
    /// </summary>
    [Serializable()]
    public class RefillCommand : ContinuousCommand
    {
        public static MSTSLocomotiveViewer Receiver { get; set; }

        public RefillCommand(CommandLog log, float? target, double startTime)
            : base(log, true, target, startTime)
        {
            Target = target;        // Fraction from 0 to 1.0
            this.Time = startTime;  // Continuous commands are created at end of change, so overwrite time when command was created
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.RefillChangeTo(Target);
            // Report();
        }
    }

    [Serializable()]
    public class ToggleOdometerCommand : Command
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
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public class ResetOdometerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ResetOdometerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerReset();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public class ToggleOdometerDirectionCommand : Command
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
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public class ToggleCylinderCocksCommand : Command {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleCylinderCocksCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if (Receiver == null) return;
            Receiver.ToggleCylinderCocks();
            // Report();
        }
    }

    // Compound Valve command
    [Serializable()]
    public class ToggleCylinderCompoundCommand : Command {
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

    // Other
    [Serializable()]
    public class ChangeCabCommand : Command {
        public static Viewer Receiver { get; set; }

        public ChangeCabCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ChangeCab();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleSwitchAheadCommand : Command {
        public static Viewer Receiver { get; set; }

        public ToggleSwitchAheadCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ToggleSwitchAhead();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleSwitchBehindCommand : Command {
        public static Viewer Receiver { get; set; }

        public ToggleSwitchBehindCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ToggleSwitchBehind();
            // Report();
        }
    }

    [Serializable()]
    public class ToggleAnySwitchCommand : IndexCommand {
        public static Viewer Receiver { get; set; }

        public ToggleAnySwitchCommand( CommandLog log, int index )
            : base(log, index)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAnySwitch(Index);
            // Report();
        }
    }

    [Serializable()]
    public class UncoupleCommand : Command {
        public static Viewer Receiver { get; set; }
        int CarPosition;    // 0 for head of train

        public UncoupleCommand( CommandLog log, int carPosition ) 
            : base( log ) {
            CarPosition = carPosition;
            Redo();
        }

        public override void Redo() {
            Receiver.UncoupleBehind( CarPosition );
            // Report();
        }

        public override string ToString() {
            return base.ToString() + " - " + CarPosition.ToString();
        }
    }

    [Serializable()]
    public class SaveScreenshotCommand : Command {
        public static Viewer Receiver { get; set; }

        public SaveScreenshotCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SaveScreenshot = true;
            // Report();
        }
    }

    [Serializable()]
    public class ResumeActivityCommand : ActivityCommand {
        public ResumeActivityCommand( CommandLog log, string eventNameLabel, double pauseDurationS )
            : base( log, eventNameLabel, pauseDurationS ) {
            Redo();
        }

        public override void Redo() {
            Receiver.ResumeActivity();
            // Report();
        }
    }

    [Serializable()]
    public class CloseAndResumeActivityCommand : ActivityCommand {
        public CloseAndResumeActivityCommand( CommandLog log, string eventNameLabel, double pauseDurationS )
            : base( log, eventNameLabel, pauseDurationS ) {
            Redo();
        }
    
        public override void Redo() {
            Receiver.CloseBox();
            // Report();
        }
    }

    [Serializable()]
    public class PauseActivityCommand : ActivityCommand
    {
        public PauseActivityCommand(CommandLog log, string eventNameLabel, double pauseDurationS)
            : base(log, eventNameLabel, pauseDurationS)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.PauseActivity();
            // Report();
        }
    }

    [Serializable()]
    public class QuitActivityCommand : ActivityCommand {
        public QuitActivityCommand( CommandLog log, string eventNameLabel, double pauseDurationS )
            : base( log, eventNameLabel, pauseDurationS ) {
            Redo();
        }

        public override void Redo() {
            Receiver.QuitActivity();
            // Report();
        }
    }
    
    [Serializable()]
    public abstract class UseCameraCommand : Command {
        public static Viewer Receiver { get; set; }

        public UseCameraCommand( CommandLog log )
            : base( log ) {
        }
    }

    [Serializable()]
    public class UseCabCameraCommand : UseCameraCommand {

        public UseCabCameraCommand( CommandLog log ) 
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.CabCamera.Activate();
            // Report();
        }
    }

	[Serializable()]
	public class Use3DCabCameraCommand : UseCameraCommand
	{

		public Use3DCabCameraCommand(CommandLog log)
			: base(log)
		{
			Redo();
		}

		public override void Redo()
		{
			Receiver.ThreeDimCabCamera.Activate();
			// Report();
		}
	}
	
	[Serializable()]
    public class UseFrontCameraCommand : UseCameraCommand {

        public UseFrontCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.FrontCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public class UseBackCameraCommand : UseCameraCommand {

        public UseBackCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.BackCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public class UseHeadOutForwardCameraCommand : UseCameraCommand {

        public UseHeadOutForwardCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.HeadOutForwardCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public class UseFreeRoamCameraCommand : UseCameraCommand {

        public UseFreeRoamCameraCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            // Makes a new free roam camera that adopts the same viewpoint as the current camera.
            // List item [0] is the current free roam camera, most recent free roam camera is at item [1]. 
            // Adds existing viewpoint to the head of the history list.
            // If this is the first use of the free roam camera, then the view point is added twice, so
            // it gets stored in the history list.
            if (Receiver.FreeRoamCameraList.Count == 0)
                Receiver.FreeRoamCameraList.Insert(0, new FreeRoamCamera(Receiver, Receiver.Camera));
            Receiver.FreeRoamCameraList.Insert(0, new FreeRoamCamera(Receiver, Receiver.Camera));
            Receiver.FreeRoamCamera.Activate();
        }
    }
    
    [Serializable()]
    public class UsePreviousFreeRoamCameraCommand : UseCameraCommand {

        public UsePreviousFreeRoamCameraCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ChangeToPreviousFreeRoamCamera();
        }
    }
    
    [Serializable()]
    public class UseHeadOutBackCameraCommand : UseCameraCommand {

        public UseHeadOutBackCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.HeadOutBackCamera.Activate();
            // Report();
        }
    }


    [Serializable()]
    public class UseBrakemanCameraCommand : UseCameraCommand {

        public UseBrakemanCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.BrakemanCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public class UsePassengerCameraCommand : UseCameraCommand {

        public UsePassengerCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.PassengerCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public class UseTracksideCameraCommand : UseCameraCommand {

        public UseTracksideCameraCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            Receiver.TracksideCamera.Activate();
            // Report();
        }
    }
    
    [Serializable()]
    public abstract class MoveCameraCommand : Command {
        public static Viewer Receiver { get; set; }
        protected double EndTime;

        public MoveCameraCommand( CommandLog log, double startTime, double endTime )
            : base( log ) {
            Time = startTime;
            EndTime = endTime;
        }

        public override string ToString() {
            return base.ToString() + " - " + String.Format( "{0}", InfoDisplay.FormattedPreciseTime( EndTime ) );
        }
    }

    [Serializable()]
    public class CameraRotateUpDownCommand : MoveCameraCommand {
        float RotationXRadians;

        public CameraRotateUpDownCommand( CommandLog log, double startTime, double endTime, float rx )
            : base( log, startTime, endTime ) {
            RotationXRadians = rx;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.RotationXTargetRadians = RotationXRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", RotationXRadians );
        }
    }


    [Serializable()]
    public class CameraRotateLeftRightCommand : MoveCameraCommand {
        float RotationYRadians;

        public CameraRotateLeftRightCommand( CommandLog log, double startTime, double endTime, float ry )
            : base( log, startTime, endTime ) {
            RotationYRadians = ry;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.RotationYTargetRadians = RotationYRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", RotationYRadians );
        }
    }

    /// <summary>
    /// Records rotations made by mouse movements.
    /// </summary>
    [Serializable()]
    public class CameraMouseRotateCommand : MoveCameraCommand {
        float RotationXRadians;
        float RotationYRadians;

        public CameraMouseRotateCommand( CommandLog log, double startTime, double endTime, float rx, float ry )
            : base( log, startTime, endTime ) {
            RotationXRadians = rx;
            RotationYRadians = ry;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.EndTime = EndTime;
                c.RotationXTargetRadians = RotationXRadians;
                c.RotationYTargetRadians = RotationYRadians;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0} {1} {2}", EndTime, RotationXRadians, RotationYRadians );
        }
    }

    [Serializable()]
    public class CameraXCommand : MoveCameraCommand {
        float XRadians;

        public CameraXCommand( CommandLog log, double startTime, double endTime, float xr )
            : base( log, startTime, endTime ) {
            XRadians = xr;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.XTargetRadians = XRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", XRadians );
        }
    }

    [Serializable()]
    public class CameraYCommand : MoveCameraCommand {
        protected float YRadians;

        public CameraYCommand( CommandLog log, double startTime, double endTime, float yr )
            : base( log, startTime, endTime ) {
            YRadians = yr;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.YTargetRadians = YRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", YRadians );
        }
    }

    [Serializable()]
    public class CameraZCommand : MoveCameraCommand {
        float ZRadians;

        public CameraZCommand( CommandLog log, double startTime, double endTime, float zr )
            : base( log, startTime, endTime ) {
            ZRadians = zr;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is RotatingCamera ) {
                var c = Receiver.Camera as RotatingCamera;
                c.ZTargetRadians = ZRadians;
                c.EndTime = EndTime;
            } // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", ZRadians );
        }
    }

	[Serializable()]
	public class CameraMoveXYZCommand : MoveCameraCommand
	{
		float X, Y, Z;

        public CameraMoveXYZCommand(CommandLog log, double startTime, double endTime, float xr, float yr, float zr)
			: base(log, startTime, endTime)
		{
			X = xr; Y = yr; Z = zr;
			Redo();
		}

		public override void Redo()
		{
			if (Receiver.Camera is ThreeDimCabCamera)
			{
				var c = Receiver.Camera as ThreeDimCabCamera;
				c.MoveCameraXYZ(X, Y, Z);
				c.EndTime = EndTime;
			}
			// Report();
		}

		public override string ToString()
		{
			return base.ToString() + String.Format(", {0}", X);
		}
	}

	[Serializable()]
    public class TrackingCameraXCommand : MoveCameraCommand {
        float PositionXRadians;

        public TrackingCameraXCommand( CommandLog log, double startTime, double endTime, float rx )
            : base( log, startTime, endTime ) {
            PositionXRadians = rx;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is TrackingCamera ) {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionXTargetRadians = PositionXRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", PositionXRadians );
        }
    }

    [Serializable()]
    public class TrackingCameraYCommand : MoveCameraCommand {
        float PositionYRadians;

        public TrackingCameraYCommand( CommandLog log, double startTime, double endTime, float ry )
            : base( log, startTime, endTime ) {
            PositionYRadians = ry;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is TrackingCamera ) {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionYTargetRadians = PositionYRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", PositionYRadians );
        }
    }

    [Serializable()]
    public class TrackingCameraZCommand : MoveCameraCommand {
        float PositionDistanceMetres;

        public TrackingCameraZCommand( CommandLog log, double startTime, double endTime, float d )
            : base( log, startTime, endTime ) {
            PositionDistanceMetres = d;
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is TrackingCamera ) {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionDistanceTargetMetres = PositionDistanceMetres;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString() {
            return base.ToString() + String.Format( ", {0}", PositionDistanceMetres );
        }
    }

    [Serializable()]
    public class NextCarCommand : UseCameraCommand {

        public NextCarCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is AttachedCamera ) {
                var c = Receiver.Camera as AttachedCamera;
                c.NextCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public class PreviousCarCommand : UseCameraCommand {

        public PreviousCarCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is AttachedCamera ) {
                var c = Receiver.Camera as AttachedCamera;
                c.PreviousCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public class FirstCarCommand : UseCameraCommand {

        public FirstCarCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is AttachedCamera ) {
                var c = Receiver.Camera as AttachedCamera;
                c.FirstCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public class LastCarCommand : UseCameraCommand {

        public LastCarCommand( CommandLog log )
            : base( log ) {
            Redo();
        }

        public override void Redo() {
            if( Receiver.Camera is AttachedCamera ) {
                var c = Receiver.Camera as AttachedCamera;
                c.LastCar();
            }
            // Report();
        }
    }

    [Serializable]
    public class FieldOfViewCommand : UseCameraCommand
    {
        float FieldOfView;

        public FieldOfViewCommand(CommandLog log, float fieldOfView)
            : base(log)
        {
            FieldOfView = fieldOfView;
            Redo();
        }

        public override void Redo()
        {
            Receiver.Camera.FieldOfView = FieldOfView;
            Receiver.Camera.ScreenChanged();
        }
    }
}
