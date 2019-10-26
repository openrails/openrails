// COPYRIGHT 2013 by the Open Rails project.
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


namespace Orts.Common
{
    public interface EventHandler
    {
        void HandleEvent(Event evt);
        void HandleEvent(Event evt, object viewer);
    }

    public enum Event
    {
        None,
        BellOff,
        BellOn,
        BlowerChange,
        BrakesStuck,
        CabLightSwitchToggle,
        CabRadioOn,
        CabRadioOff,
        CircuitBreakerOpen,
        CircuitBreakerClosing,
        CircuitBreakerClosed,
        CircuitBreakerClosingOrderOff,
        CircuitBreakerClosingOrderOn,
        CircuitBreakerOpeningOrderOff,
        CircuitBreakerOpeningOrderOn,
        CircuitBreakerClosingAuthorizationOff,
        CircuitBreakerClosingAuthorizationOn,
        CompressorOff,
        CompressorOn,
        ControlError,
        Couple,
        CoupleB, // NOTE: Currently not used in Open Rails.
        CoupleC, // NOTE: Currently not used in Open Rails.
        CrossingClosing,
        CrossingOpening,
        CylinderCocksToggle,
        CylinderCompoundToggle,
        DamperChange,
        Derail1, // NOTE: Currently not used in Open Rails.
        Derail2, // NOTE: Currently not used in Open Rails.
        Derail3, // NOTE: Currently not used in Open Rails.
        DoorClose,
        DoorOpen,
        DynamicBrakeChange,
        DynamicBrakeIncrease, // NOTE: Currently not used in Open Rails.
        DynamicBrakeOff,
        EngineBrakeChange,
        EngineBrakePressureDecrease,
        EngineBrakePressureIncrease,
        EnginePowerOff, 
        EnginePowerOn, 
        FireboxDoorChange,
        FireboxDoorOpen,
        FireboxDoorClose,
        FuelTowerDown,
        FuelTowerTransferEnd,
        FuelTowerTransferStart,
        FuelTowerUp,
        GearDown,
        GearUp,
        HornOff,
        HornOn,
        LightSwitchToggle,
        MirrorClose, 
        MirrorOpen, 
        Pantograph1Down,
        PantographToggle,
        Pantograph1Up,
        Pantograph2Down,
        Pantograph2Up,
        Pantograph3Down,
        Pantograph3Up,
        Pantograph4Down,
        Pantograph4Up,
        PermissionDenied,
        PermissionGranted,
        PermissionToDepart,
        ReverserChange,
        ReverserToForwardBackward,
        ReverserToNeutral,
        SanderOff,
        SanderOn,
        SemaphoreArm,
        LargeEjectorChange,
        SmallEjectorChange,
        WaterInjector1Off,
        WaterInjector1On,
        WaterInjector2Off,
        WaterInjector2On,
        SteamHeatChange, 
        SteamPulse1,
        SteamPulse2,
        SteamPulse3,
        SteamPulse4,
        SteamPulse5,
        SteamPulse6,
        SteamPulse7,
        SteamPulse8,
        SteamPulse9,
        SteamPulse10,
        SteamPulse11,
        SteamPulse12,
        SteamPulse13,
        SteamPulse14,
        SteamPulse15,
        SteamPulse16,
        SteamSafetyValveOff,
        SteamSafetyValveOn,
        ThrottleChange,
        TrainBrakeChange,
        TrainBrakePressureDecrease,
        TrainBrakePressureIncrease,
        TrainControlSystemActivate,
        TrainControlSystemAlert1,
        TrainControlSystemAlert2,
        TrainControlSystemDeactivate,
        TrainControlSystemInfo1,
        TrainControlSystemInfo2,
        TrainControlSystemPenalty1,
        TrainControlSystemPenalty2,
        TrainControlSystemWarning1,
        TrainControlSystemWarning2,
        MovingTableMovingEmpty,
        MovingTableMovingLoaded,
        MovingTableStopped,
        Uncouple,
        UncoupleB, // NOTE: Currently not used in Open Rails.
        UncoupleC, // NOTE: Currently not used in Open Rails.
        VacuumExhausterOn,
        VacuumExhausterOff,
        VigilanceAlarmOff,
        VigilanceAlarmOn,
        VigilanceAlarmReset,
        WaterScoopDown,
        WaterScoopUp,
        WiperOff,
        WiperOn,
        _HeadlightDim,
        _HeadlightOff,
        _HeadlightOn,
        _ResetWheelSlip,

        TrainBrakePressureStoppedChanging,
        EngineBrakePressureStoppedChanging,
        BrakePipePressureIncrease,
        BrakePipePressureDecrease,
        BrakePipePressureStoppedChanging,
        CylinderCocksOpen,
        CylinderCocksClose,
        SecondEnginePowerOff,
        SecondEnginePowerOn,

        HotBoxBearingOn,
        HotBoxBearingOff
    }

    public static class Events
    {
        public enum Source
        {
            None,
            MSTSCar,
            MSTSCrossing,
            MSTSFuelTower,
            MSTSInGame,
            MSTSSignal,
            ORTSTurntable
        }

        // PLEASE DO NOT EDIT THESE FUNCTIONS without references and testing!
        // These numbers are the MSTS sound triggers and must match
        // MSTS/MSTSBin behaviour whenever possible. NEVER return values for
        // non-MSTS events when passed an MSTS Source.

        public static Event From(bool mstsBinEnabled, Source source, int eventID)
        {
            switch (source)
            {
                case Source.MSTSCar:
                    if (mstsBinEnabled)
                    {
                        switch (eventID)
                        {
                            // MSTSBin codes (documented at http://mstsbin.uktrainsim.com/)
                            case 23: return Event.EnginePowerOn;
                            case 24: return Event.EnginePowerOff;
                            case 66: return Event.Pantograph2Up;
                            case 67: return Event.Pantograph2Down;
                            default: break;
                        }
                    }
                    switch (eventID)
                    {
                        // Calculated from inspection of existing engine .sms files and extensive testing.
                        // Event 1 is unused in MSTS.
                        case 2: return Event.DynamicBrakeIncrease;
                        case 3: return Event.DynamicBrakeOff;
                        case 4: return Event.SanderOn;
                        case 5: return Event.SanderOff;
                        case 6: return Event.WiperOn;
                        case 7: return Event.WiperOff;
                        case 8: return Event.HornOn;
                        case 9: return Event.HornOff;
                        case 10: return Event.BellOn;
                        case 11: return Event.BellOff;
                        case 12: return Event.CompressorOn;
                        case 13: return Event.CompressorOff;
                        case 14: return Event.TrainBrakePressureIncrease;
                        case 15: return Event.ReverserChange;
                        case 16: return Event.ThrottleChange;
                        case 17: return Event.TrainBrakeChange; // Event 17 only works first time in MSTS.
                        case 18: return Event.EngineBrakeChange; // Event 18 only works first time in MSTS; MSTSBin fixes this.
                        // Event 19 is unused in MSTS.
                        case 20: return Event.DynamicBrakeChange;
                        case 21: return Event.EngineBrakePressureIncrease; // Event 21 is defined in sound files but never used in MSTS.
                        case 22: return Event.EngineBrakePressureDecrease; // Event 22 is defined in sound files but never used in MSTS.
                        // Event 23 is unused in MSTS.
                        // Event 24 is unused in MSTS.
                        // Event 25 is possibly a vigilance reset in MSTS sound files but is never used.
                        // Event 26 is a sander toggle in MSTS sound files but is never used.
                        case 27: return Event.WaterInjector2On;
                        case 28: return Event.WaterInjector2Off;
                        // Event 29 is unused in MSTS.
                        case 30: return Event.WaterInjector1On;
                        case 31: return Event.WaterInjector1Off;
                        case 32: return Event.DamperChange;
                        case 33: return Event.BlowerChange;
                        case 34: return Event.CylinderCocksToggle;
                        // Event 35 is unused in MSTS.
                        case 36: return Event.FireboxDoorChange;
                        case 37: return Event.LightSwitchToggle;
                        case 38: return Event.WaterScoopDown;
                        case 39: return Event.WaterScoopUp;
                        case 40: return Event.FireboxDoorOpen; // Used in default steam locomotives (Scotsman and 380)
                        case 41: return Event.FireboxDoorClose;
                        case 42: return Event.SteamSafetyValveOn;
                        case 43: return Event.SteamSafetyValveOff;
                        case 44: return Event.SteamHeatChange; // Event 44 only works first time in MSTS.
                        case 45: return Event.Pantograph1Up;
                        case 46: return Event.Pantograph1Down;
                        case 47: return Event.PantographToggle;
                        case 48: return Event.VigilanceAlarmReset;
                        // Event 49 is unused in MSTS.
                        // Event 50 is unused in MSTS.
                        // Event 51 is an engine brake of some kind in MSTS sound files but is never used.
                        // Event 52 is unused in MSTS.
                        // Event 53 is a train brake normal apply in MSTS sound files but is never used.
                        case 54: return Event.TrainBrakePressureDecrease; // Event 54 is a train brake emergency apply in MSTS sound files but is actually a train brake pressure decrease.
                        // Event 55 is unused in MSTS.
                        case 56: return Event.VigilanceAlarmOn;
                        case 57: return Event.VigilanceAlarmOff; // Event 57 is triggered constantly in MSTS when the vigilance alarm is off.
                        case 58: return Event.Couple;
                        case 59: return Event.CoupleB;
                        case 60: return Event.CoupleC;
                        case 61: return Event.Uncouple;
                        case 62: return Event.UncoupleB;
                        case 63: return Event.UncoupleC;
                        // Event 64 is unused in MSTS.

                        // ORTS only Events
                        case 101: return Event.GearUp; // for gearbox based engines
                        case 102: return Event.GearDown; // for gearbox based engines
                        case 103: return Event.ReverserToForwardBackward; // reverser moved to forward or backward position
                        case 104: return Event.ReverserToNeutral; // reversed moved to neutral
                        case 105: return Event.DoorOpen; // door opened; propagated to all locos and wagons of the consist
                        case 106: return Event.DoorClose; // door closed; propagated to all locos and wagons of the consist
                        case 107: return Event.MirrorOpen; 
                        case 108: return Event.MirrorClose;
                        case 109: return Event.TrainControlSystemInfo1;
                        case 110: return Event.TrainControlSystemInfo2;
                        case 111: return Event.TrainControlSystemActivate;
                        case 112: return Event.TrainControlSystemDeactivate;
                        case 113: return Event.TrainControlSystemPenalty1;
                        case 114: return Event.TrainControlSystemPenalty2;
                        case 115: return Event.TrainControlSystemWarning1;
                        case 116: return Event.TrainControlSystemWarning2;
                        case 117: return Event.TrainControlSystemAlert1;
                        case 118: return Event.TrainControlSystemAlert2;
                        case 119: return Event.CylinderCompoundToggle; // Locomotive switched to compound
                        
                        case 121: return Event.SteamPulse1;
                        case 122: return Event.SteamPulse2;
                        case 123: return Event.SteamPulse3;
                        case 124: return Event.SteamPulse4;
                        case 125: return Event.SteamPulse5;
                        case 126: return Event.SteamPulse6;
                        case 127: return Event.SteamPulse7;
                        case 128: return Event.SteamPulse8;
                        case 129: return Event.SteamPulse9;
                        case 130: return Event.SteamPulse10;
                        case 131: return Event.SteamPulse11;
                        case 132: return Event.SteamPulse12;
                        case 133: return Event.SteamPulse13;
                        case 134: return Event.SteamPulse14;
                        case 135: return Event.SteamPulse15;
                        case 136: return Event.SteamPulse16;

                        case 137: return Event.CylinderCocksOpen;
                        case 138: return Event.CylinderCocksClose;
                        case 139: return Event.TrainBrakePressureStoppedChanging;
                        case 140: return Event.EngineBrakePressureStoppedChanging;
                        case 141: return Event.BrakePipePressureIncrease;
                        case 142: return Event.BrakePipePressureDecrease;
                        case 143: return Event.BrakePipePressureStoppedChanging;

                        case 150: return Event.CircuitBreakerOpen;
                        case 151: return Event.CircuitBreakerClosing;
                        case 152: return Event.CircuitBreakerClosed;
                        case 153: return Event.CircuitBreakerClosingOrderOn;
                        case 154: return Event.CircuitBreakerClosingOrderOff;
                        case 155: return Event.CircuitBreakerOpeningOrderOn;
                        case 156: return Event.CircuitBreakerOpeningOrderOff;
                        case 157: return Event.CircuitBreakerClosingAuthorizationOn;
                        case 158: return Event.CircuitBreakerClosingAuthorizationOff;

                        case 159: return Event.LargeEjectorChange;
                        case 160: return Event.SmallEjectorChange;

                        case 161: return Event.CabLightSwitchToggle;
                        case 162: return Event.CabRadioOn;
                        case 163: return Event.CabRadioOff;

                        case 164: return Event.BrakesStuck;

                        case 165: return Event.VacuumExhausterOn;
                        case 166: return Event.VacuumExhausterOff;
                        case 167: return Event.SecondEnginePowerOn;
                        case 168: return Event.SecondEnginePowerOff;

                        case 169: return Event.Pantograph3Up;
                        case 170: return Event.Pantograph3Down;
                        case 171: return Event.Pantograph4Up;
                        case 172: return Event.Pantograph4Down;

                        case 173: return Event.HotBoxBearingOn;
                        case 174: return Event.HotBoxBearingOff;
                        //

                        default: return 0;
                    }
                case Source.MSTSCrossing:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing crossing.sms files.
                        case 3: return Event.CrossingClosing;
                        case 4: return Event.CrossingOpening;
                        default: return 0;
                    }
                case Source.MSTSFuelTower:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing *tower.sms files.
                        case 6: return Event.FuelTowerDown;
                        case 7: return Event.FuelTowerUp;
                        case 9: return Event.FuelTowerTransferStart;
                        case 10: return Event.FuelTowerTransferEnd;
                        default: return 0;
                    }
                case Source.MSTSInGame:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing ingame.sms files.
                        case 10: return Event.ControlError;
                        case 20: return Event.Derail1;
                        case 21: return Event.Derail2;
                        case 22: return Event.Derail3;
                        case 25: return 0; // TODO: What is this event?
                        case 60: return Event.PermissionToDepart;
                        case 61: return Event.PermissionGranted;
                        case 62: return Event.PermissionDenied;
                        default: return 0;
                    }
                case Source.MSTSSignal:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing signal.sms files.
                        case 1: return Event.SemaphoreArm;
                        default: return 0;
                    }
                case Source.ORTSTurntable:
                    switch (eventID)
                    {
                        // related file is turntable.sms
                        case 1: return Event.MovingTableMovingEmpty;
                        case 2: return Event.MovingTableMovingLoaded;
                        case 3: return Event.MovingTableStopped;
                        default: return 0;
                    }
                default: return 0;
            }
        }
    }
}
