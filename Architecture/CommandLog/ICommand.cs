using System;   // Needed for TimeSpan

namespace ORArchitecture.CommandLog {
    public interface ICommand {
        
        // Tested using .Net's System.TimeSpan. Needs converting to simulator time.
        TimeSpan Time { get; set; }

        // bool IsEnabled { get; set; } //  Can't specify this as implementation is static
        // bool IsUndoable { get; }     //  Can't specify this as implementation is static

        /// <summary>
        /// Call the Receiver to reverse the command.
        /// </summary>
        void Undo();

        /// <summary>
        /// Call the Receiver to repeat the Command
        /// </summary>
        void Redo();
    }
}
