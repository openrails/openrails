using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORArchitecture
{
    /// <summary>
    /// A mechanism for communicaton among modules.
    /// </summary>
    interface iListener
    {
        public void RecieveMessage( Messaging source, EventMessage message);
    }

    interface iSender
    {
        public void SendMessage(Messaging source, Messaging destination, EventMessage message );
        // for example signal code might send a message to a train driver
        //              SendMessage( this, Train4.Controller, EventMessage( "Hold at siding 7 for train 16" ) )
    }

    class EventMessage
    {
        // TO DEFINE
    }

}
