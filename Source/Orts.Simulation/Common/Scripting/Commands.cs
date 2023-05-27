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

using System;
using Orts.Common;
using Orts.Simulation.RollingStocks.SubSystems;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// This is the list of commands available for TCS scripts; they are generic commands, whose action will specified by the active script
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

    // Generic TCS button command
    [Serializable()]
    public sealed class TCSButtonCommand : BooleanCommand
    {
        public int CommandIndex;
        public static ScriptedTrainControlSystem Receiver { get; set; }

        public TCSButtonCommand(CommandLog log, bool toState, int commandIndex)
            : base(log, toState)
        {
            CommandIndex = commandIndex;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null)
            {
                Receiver.TCSCommandButtonDown[CommandIndex] = ToState;
                Receiver.HandleEvent(ToState ? TCSEvent.GenericTCSButtonPressed : TCSEvent.GenericTCSButtonReleased, CommandIndex);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "on" : "off");
        }
    }

    // Generic TCS switch command
    [Serializable()]
    public sealed class TCSSwitchCommand : BooleanCommand
    {
        public int CommandIndex;
        public static ScriptedTrainControlSystem Receiver { get; set; }

        public TCSSwitchCommand(CommandLog log, bool toState, int commandIndex)
            : base(log, toState)
        {
            CommandIndex = commandIndex;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null)
            {
                Receiver.TCSCommandSwitchOn[CommandIndex] = ToState;
                Receiver.HandleEvent(ToState ? TCSEvent.GenericTCSSwitchOn : TCSEvent.GenericTCSSwitchOff, CommandIndex);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "on" : "off");
        }
    }
}
