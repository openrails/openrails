using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public interface CarEventHandler
    {
        void HandleCarEvent(EventID eventID);
    }

    public enum EventID {

        SanderOn = 4, SanderOff = 5,
        WiperOn = 6, WiperOff = 7,
        HornOn = 8, HornOff = 9,
        BellOn = 10, BellOff = 11,
        HeadlightOn = 12, HeadlightDim = 13, HeadlightOff = 14, 
        Forward = 15, Reverse = 16,
        PantographUp = 45, PantographDown = 46, PantographToggle = 47,
        Couple = 58, CoupleB = 59, CoupleC = 60,
        Uncouple = 61, UncoupleB = 62, UncoupleC = 63, 

    }
}
