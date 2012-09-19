namespace ORArchitecture
{
    /// <summary>
    /// The base viewer class would have capability similar to a 
    /// real world train watcher.  It can move around and view the VirtualWorld, 
    /// but is assigned no control interfaces.  
    /// A viewer must implement the iListener interface to enable it to receive 
    /// messages from the other components in the system
    /// </summary>
    interface Viewer: IListener
    {
        void Viewer( IVirtualWorld virtualWorld );
    }


    /// <summary>
    /// Adds capability for controlling a train, including handling keyboard and mouse
    /// inputs and translating them to trainController commands, and graphics representations
    /// such as cab view etc.
    /// </summary>
    interface TrainDriver : Viewer
    {
        void TrainDriver( IVirtualWorld virtualWorld, IControlTrain trainController);
    }

    /// <summary>
    /// A extension of the concept.  For example a player could control the gantry crane
    /// at a container loading port 
    /// </summary>
    interface CraneDriver : Viewer
    {
        void CraneDriver( IVirtualWorld virtualWorld, IControlCrane craneController);
    }

    interface EditorView : Viewer
    {
        void EditorView( IVirtualWorld virtualWorld, IControlTerrain terrainController, IControlScenery sceneryController);
    }

}
