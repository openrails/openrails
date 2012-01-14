using System;
using System.Collections.Generic;   // needed for List

namespace ORArchitecture.CommandLog {
    interface ICommandLog {

        /* Hidden as interfaces cannot declare types such as CommandActivator
        /// <summary>
        /// Links a button or menu option to a subclass of Command.
        /// </summary>
        struct CommandActivator {
            UIElement Activator;
            Type CommandType;
        }

        /// <summary>
        /// All the buttons, menu options in the system.
        /// </summary>
        List<CommandActivator> ActivatorList { get; set; }
        */

        /// <summary>
        /// Registers a subclass of command and the receiver object that will action the command.
        /// e.g. commandLog.RegisterCommand( typeof(InsertTextCommand), receiver, true, true );
        /// </summary>
        /// <param name="commandClass"></param>
        /// <param name="receiver"></param>
        /// <param name="initialStateIsEnabled"></param>
        /// <param name="isUndoable"></param>
        void RegisterCommand( Type commandClass, IReceiver receiver, bool initialStateIsEnabled, bool isUndoable );

        /// <summary>
        /// Registers an activator (i.e. button or menu option) and the subclass of command object it creates.
        /// e.g. commandLog.RegisterActivator( typeof(InsertTextCommand), bInsertText );
        /// </summary>
        /// <param name="commandClass"></param>
        /// <param name="uIElement"></param>
        void RegisterActivator( Type commandClass, object uIElement );  // "object" substituted for "UIElement" (part of WPF) to allow compilation

        /// <summary>
        /// When a command is created, it adds itself to the log. <br />
        /// </summary>
        /// <param name="Command"></param>
        void CommandAdd( ICommand Command );

        /// <summary>
        /// At the end of a command, this sets the state of all buttons and menu options in ActivatorList.
        /// </summary>
        void EnableActivators();
        
        /// <summary>
        /// Reverse the most recent command.
        /// </summary>
        void Undo();

        /// <summary>
        /// Repeat the command which was just undone.
        /// </summary>
        void Redo();

        /// <summary>
        /// Issues all the commands in the command log at the intervals that they were originally issued.
        /// This replay can also be paused, resumed or stopped.
        /// </summary>
        void Replay();
        void Stop();
        void Pause();
        void Resume();
        
        /// <summary>
        /// Copies the command objects from the log into the file specified, first creating the file.
        /// </summary>
        /// <param name="fullFilePath"></param>
        void SaveLog(string filePath);
        
        /// <summary>
        /// Copies the command objects from the file specified into the log, first emptying the log.
        /// </summary>
        /// <param name="fullFilePath"></param>
        void LoadLog( string filePath );
    }
}

