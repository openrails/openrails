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


using System;

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
        AITrainApproachingStation,
        AITrainHelperLoco,
        AITrainLeadLoco,
        AITrainLeavingStation,
        PlayerTrainHelperLoco,
        PlayerTrainLeadLoco,
        StaticTrainLoco,
        EndAITrainLeadLoco,
        BatterySwitchOff,
        BatterySwitchOn,
        BatterySwitchCommandOff,
        BatterySwitchCommandOn,
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
        CraneXAxisMove,
        CraneXAxisSlowDown,
        CraneYAxisMove,
        CraneYAxisSlowDown,
        CraneZAxisMove,
        CraneZAxisSlowDown,
        CraneYAxisBumpsOnTarget,
        CraneYAxisDown,
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
        ElectricTrainSupplyOff,
        ElectricTrainSupplyOn,
        ElectricTrainSupplyCommandOff,
        ElectricTrainSupplyCommandOn,
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
        GenericEvent1,
        GenericEvent2,
        GenericEvent3,
        GenericEvent4,
        GenericEvent5,
        GenericEvent6,
        GenericEvent7,
        GenericEvent8,
        GenericItem1On,
        GenericItem1Off,
        GenericItem2On,
        GenericItem2Off,
        HornOff,
        HornOn,
        LightSwitchToggle,
        MasterKeyOff,
        MasterKeyOn,
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
        VoltageSelectorDecrease,
        VoltageSelectorIncrease,
        PantographSelectorDecrease,
        PantographSelectorIncrease,
        PowerLimitationSelectorDecrease,
        PowerLimitationSelectorIncrease,
        ReverserChange,
        ReverserToForwardBackward,
        ReverserToNeutral,
        SanderOff,
        SanderOn,
        SemaphoreArm,
        ServiceRetentionButtonOff,
        ServiceRetentionButtonOn,
        ServiceRetentionCancellationButtonOff,
        ServiceRetentionCancellationButtonOn,
        LargeEjectorChange,
        SmallEjectorChange,
        WaterInjector1Off,
        WaterInjector1On,
        WaterInjector2Off,
        WaterInjector2On,
        WaterMotionPump1Off,
        WaterMotionPump1On,
        WaterMotionPump2Off,
        WaterMotionPump2On,
        BlowdownValveToggle,
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
        TakeScreenshot,
        ThrottleChange,
        TractionCutOffRelayOpen,
        TractionCutOffRelayClosing,
        TractionCutOffRelayClosed,
        TractionCutOffRelayClosingOrderOff,
        TractionCutOffRelayClosingOrderOn,
        TractionCutOffRelayOpeningOrderOff,
        TractionCutOffRelayOpeningOrderOn,
        TractionCutOffRelayClosingAuthorizationOff,
        TractionCutOffRelayClosingAuthorizationOn,
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
        WindowClosing,
        WindowOpening,
        WindowsClosed,
        WindowsOpen,
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
        BoosterCylinderCocksOpen,
        BoosterCylinderCocksClose,
        SecondEnginePowerOff,
        SecondEnginePowerOn,

        CounterPressureBrakeOn,
        CounterPressureBrakeOff,

        HotBoxBearingOn,
        HotBoxBearingOff,

        BoilerBlowdownOn,
        BoilerBlowdownOff,

        WaterScoopRaiseLower,
        WaterScoopBroken,

        SteamGearLeverToggle,
        AIFiremanSoundOn,
        AIFiremanSoundOff,

        GearPosition0,
        GearPosition1,
        GearPosition2,
        GearPosition3,
        GearPosition4,
        GearPosition5,
        GearPosition6,
        GearPosition7,
        GearPosition8,

        LargeEjectorOn,
        LargeEjectorOff,
        SmallEjectorOn,
        SmallEjectorOff,

        PowerConverterOff,
        PowerConverterOn,
        VentilationOff,
        VentilationLow,
        VentilationHigh,
        HeatingOff,
        HeatingOn,
        AirConditioningOff,
        AirConditioningOn,

        OverchargeBrakingOn,
        OverchargeBrakingOff,
        EmergencyVentValveOn,

        // Cruise Control
        LeverFromZero,
        LeverToZero,
        CruiseControlSpeedRegulator,
        CruiseControlSpeedSelector,
        CruiseControlMaxForce,
        CruiseControlAlert,
        CruiseControlAlert1,

        // request stop
        RequestStopAnnounce,

        MPCChangePosition,

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
            ORTSTurntable,
            ORTSContainerCrane
        }

        // PLEASE DO NOT EDIT THESE FUNCTIONS without references and testing!
        // These numbers are the MSTS sound triggers and must match
        // MSTS/MSTSBin behaviour whenever possible. NEVER return values for
        // non-MSTS events when passed an MSTS Source.

        public static Event From(Source source, int eventID)
        {
            switch (source)
            {
                case Source.MSTSCar:
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
                        // MSTSBin codes 23, 24 (documented at http://mstsbin.uktrainsim.com/)
                        case 23: return Event.EnginePowerOn;
                        case 24: return Event.EnginePowerOff;
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
                        // MSTSBin codes 66, 67 (documented at http://mstsbin.uktrainsim.com/)
                        case 66: return Event.Pantograph2Up;
                        case 67: return Event.Pantograph2Down;


                        // ORTS only Events
                        case 90: return Event.WaterMotionPump1On;
                        case 91: return Event.WaterMotionPump1Off;
                        case 92: return Event.WaterMotionPump2On;
                        case 93: return Event.WaterMotionPump2Off;

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

                        case 120: return Event.BlowdownValveToggle;
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

                        case 145: return Event.WaterScoopRaiseLower;
                        case 146: return Event.WaterScoopBroken;

                        case 147: return Event.SteamGearLeverToggle;
                        case 148: return Event.AIFiremanSoundOn;
                        case 149: return Event.AIFiremanSoundOff;

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

                        case 175: return Event.BoilerBlowdownOn;
                        case 176: return Event.BoilerBlowdownOff;

                        case 181: return Event.GenericEvent1;
                        case 182: return Event.GenericEvent2;
                        case 183: return Event.GenericEvent3;
                        case 184: return Event.GenericEvent4;
                        case 185: return Event.GenericEvent5;
                        case 186: return Event.GenericEvent6;
                        case 187: return Event.GenericEvent7;
                        case 188: return Event.GenericEvent8;

                        case 189: return Event.BatterySwitchOn;
                        case 190: return Event.BatterySwitchOff;
                        case 191: return Event.BatterySwitchCommandOn;
                        case 192: return Event.BatterySwitchCommandOff;

                        case 193: return Event.MasterKeyOn;
                        case 194: return Event.MasterKeyOff;

                        case 195: return Event.ServiceRetentionButtonOn;
                        case 196: return Event.ServiceRetentionButtonOff;
                        case 197: return Event.ServiceRetentionCancellationButtonOn;
                        case 198: return Event.ServiceRetentionCancellationButtonOff;

                        case 200: return Event.GearPosition0;
                        case 201: return Event.GearPosition1;
                        case 202: return Event.GearPosition2;
                        case 203: return Event.GearPosition3;
                        case 204: return Event.GearPosition4;
                        case 205: return Event.GearPosition5;
                        case 206: return Event.GearPosition6;
                        case 207: return Event.GearPosition7;
                        case 208: return Event.GearPosition8;

                        case 210: return Event.LargeEjectorOn;
                        case 211: return Event.LargeEjectorOff;
                        case 212: return Event.SmallEjectorOn;
                        case 213: return Event.SmallEjectorOff;

                        case 214: return Event.TractionCutOffRelayOpen;
                        case 215: return Event.TractionCutOffRelayClosing;
                        case 216: return Event.TractionCutOffRelayClosed;
                        case 217: return Event.TractionCutOffRelayClosingOrderOn;
                        case 218: return Event.TractionCutOffRelayClosingOrderOff;
                        case 219: return Event.TractionCutOffRelayOpeningOrderOn;
                        case 220: return Event.TractionCutOffRelayOpeningOrderOff;
                        case 221: return Event.TractionCutOffRelayClosingAuthorizationOn;
                        case 222: return Event.TractionCutOffRelayClosingAuthorizationOff;

                        case 223: return Event.ElectricTrainSupplyOn;
                        case 224: return Event.ElectricTrainSupplyOff;
                        case 225: return Event.ElectricTrainSupplyCommandOn;
                        case 226: return Event.ElectricTrainSupplyCommandOff;

                        case 227: return Event.PowerConverterOn;
                        case 228: return Event.PowerConverterOff;
                        case 229: return Event.VentilationHigh;
                        case 230: return Event.VentilationLow;
                        case 231: return Event.VentilationOff;
                        case 232: return Event.HeatingOn;
                        case 233: return Event.HeatingOff;
                        case 234: return Event.AirConditioningOn;
                        case 235: return Event.AirConditioningOff;

                        case 240: return Event.GenericItem1On;
                        case 241: return Event.GenericItem1Off;
                        case 242: return Event.GenericItem2On;
                        case 243: return Event.GenericItem2Off;

                        case 250: return Event.OverchargeBrakingOn;
                        case 251: return Event.OverchargeBrakingOff;
                        case 252: return Event.EmergencyVentValveOn;

                        case 253: return Event.VoltageSelectorDecrease;
                        case 254: return Event.VoltageSelectorIncrease;
                        case 255: return Event.PantographSelectorDecrease;
                        case 256: return Event.PantographSelectorIncrease;
                        case 257: return Event.PowerLimitationSelectorDecrease;
                        case 258: return Event.PowerLimitationSelectorIncrease;

                        case 260: return Event.WindowClosing;
                        case 261: return Event.WindowOpening;
                        case 262: return Event.WindowsClosed;
                        case 263: return Event.WindowsOpen;

                        case 270: return Event.RequestStopAnnounce;

                        // Cruise Control
                        case 298: return Event.LeverFromZero;
                        case 299: return Event.LeverToZero;
                        case 300: return Event.CruiseControlSpeedRegulator;
                        case 301: return Event.CruiseControlSpeedSelector;
                        case 302: return Event.CruiseControlMaxForce;
                        case 303: return Event.CruiseControlAlert;
                        case 304: return Event.CruiseControlAlert1;

                        case 310: return Event.MPCChangePosition;

                        case 321: return Event.BoosterCylinderCocksOpen;
                        case 322: return Event.BoosterCylinderCocksClose;

                        case 323: return Event.CounterPressureBrakeOn;
                        case 324: return Event.CounterPressureBrakeOff;

                        // AI train related events
                        case 330: return Event.AITrainLeadLoco;
                        case 331: return Event.AITrainHelperLoco;
                        case 332: return Event.PlayerTrainLeadLoco;
                        case 333: return Event.PlayerTrainHelperLoco;
                        case 334: return Event.AITrainApproachingStation;
                        case 335: return Event.AITrainLeavingStation;
                        case 336: return Event.StaticTrainLoco;
                        case 337: return Event.EndAITrainLeadLoco;
						
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
                case Source.ORTSContainerCrane:
                    switch (eventID)
                    {
                        // Can be different from crane to crane
                        case 1: return Event.CraneXAxisMove;
                        case 2: return Event.CraneXAxisSlowDown;
                        case 3: return Event.CraneYAxisMove;
                        case 4: return Event.CraneYAxisSlowDown;
                        case 5: return Event.CraneZAxisMove;
                        case 6: return Event.CraneZAxisSlowDown;
                        case 7: return Event.CraneYAxisDown;
                        default: return 0;
                    }
                default: return 0;
            }
        }
    }
}
