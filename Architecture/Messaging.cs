using System;
using System.Collections.Generic;

namespace ORArchitecture
{
    /// <summary>
    /// A mechanism for communicaton among modules.
    /// </summary>
    interface IListener
    {
        void ReceiveMessage( Messaging source, EventMessage message);
    }

    interface ISender
    {
        void SendMessage(Messaging source, Messaging destination, EventMessage message );
        // for example signal code might send a message to a train driver
        //              SendMessage( this, Train4.Controller, EventMessage( "Hold at siding 7 for train 16" ) )
    }

    class Messaging {
        // TO DEFINE
    }

    class EventMessage
    {
        // TO DEFINE
    }

}
