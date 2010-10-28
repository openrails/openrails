
namespace ORTS
{
    public interface CarEventHandler
    {
        void HandleCarEvent(EventID eventID);
    }

    public class EventID
    {
        public static int SanderOn = 4, SanderOff = 5,
        WiperOn = 6, WiperOff = 7,
        HornOn = 8, HornOff = 9,
        BellOn = 10, BellOff = 11,
        CompressorOn = 12, CompressorOff = 13, TrainBrakeRelease = 14,
        Forward = 15, Reverse = 16, TrainBrakeSet = 17, EngineBrakeSet = 18,
        EngineBrakeRelease = 21, EngineBrakeApply = 22, LightSwitchToggle = 37,
        PantographUp = 45, PantographDown = 46, PantographToggle = 47, PowerHandler = 50,
        TrainBrakeApply = 53, TrainBrakeEmergency = 54,
        Couple = 58, CoupleB = 59, CoupleC = 60,
        Uncouple = 61, UncoupleB = 62, UncoupleC = 63,
            // why do these headlight values overlap brake sounds?
            //HeadlightOn = 12, HeadlightDim = 13, HeadlightOff = 14,
        HeadlightOn = 112, HeadlightDim = 113, HeadlightOff = 114;

        public static bool IsMSTSBin = false;

        public static void SetMSTSBINCompatible()
        {
            TrainBrakeEmergency = 51;
            TrainBrakeApply = 14;
            TrainBrakeRelease = 54;
            Reverse = 15;
            PowerHandler = 16;
            IsMSTSBin = true;
        }
       
        private int _id;
        public EventID(int ID)
        {
            _id = ID;
        }
        
        public static implicit operator EventID (int ID)
        {
            return new EventID(ID);
        }

        public static implicit operator int(EventID EvtID)
        {
            return EvtID._id;
        }

        public static bool operator == (EventID ev1, EventID ev2)
        {
            return ev1._id == ev2._id;
        }

        public static bool operator !=(EventID ev1, EventID ev2)
        {
            return ev1._id != ev2._id;
        }

        public override int GetHashCode()
        {
            return _id;
        }
        public override bool Equals(object obj)
        {
            if (obj is int) return (int)obj == _id;
            return base.Equals(obj);
        }
    }
/*
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
 */
}
