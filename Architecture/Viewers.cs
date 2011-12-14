using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORArchitecture
{
    /// <summary>
    /// The base viewer can The base viewer class would have capability similar to a 
    /// real world train watcher.  It can move around and view the VirtualWorld, 
    /// but is assigned no control interfaces.  
    /// A viewer must implement the iListener interface to enable it to recieve 
    /// messages from the other components in the system
    /// </summary>
    interface Viewer: IListener
    {
        Viewer( iVirtualWorld virtualWorld );
    }


    /// <summary>
    /// Adds capability for controlling a train, including handling keyboard and mouse
    /// inputs and translating them to trainController commands, and graphics representations
    /// such as cab view etc.
    /// </summary>
    interface TrainDriver : Viewer
    {
        TrainDriver( iVirtualWorld virtualWorld, iControlTrain trainController);
    }

    /// <summary>
    /// A extension of the concept.  For example a player could control the gantry crane
    /// at a container loading port 
    /// </summary>
    interface CraneDriver : Viewer
    {
        CraneDriver( iVirtualWorld virtualWorld, iControlCrane craneController);
    }

    interface EditorView : Viewer
    {
        EditorView( iVirtualWorld virtualWorld, iControlTerrain terrainController, iControlScenery sceneryController);
    }

}
