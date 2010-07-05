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
        CompressorOn = 12, CompressorOff = 13, TrainBrakeRelease = 14,
        Forward = 15, Reverse = 16,
        EngineBrakeRelease = 21, EngineBrakeApply = 22,
        PantographUp = 45, PantographDown = 46, PantographToggle = 47,
        TrainBrakeApply = 53, TrainBrakeEmergency = 54,
        Couple = 58, CoupleB = 59, CoupleC = 60,
        Uncouple = 61, UncoupleB = 62, UncoupleC = 63,
        // why do these headlight values overlap brake sounds?
        //HeadlightOn = 12, HeadlightDim = 13, HeadlightOff = 14,
        HeadlightOn = 112, HeadlightDim = 113, HeadlightOff = 114,

    }
}
