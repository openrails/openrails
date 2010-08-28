using System;
using System.IO;

namespace ORTS
{
    public enum ControllerTypes
    {        
        MSTSNotchController = 1,        
        MSTSBrakeController
    }

    public class ControllerFactory
    {
        public static void Save(IController controller, BinaryWriter outf)
        {
            outf.Write(controller != null);

            if (controller != null)
                controller.Save(outf);
        }

        public static IController Restore(BinaryReader inf)
        {
            if (!inf.ReadBoolean())
                return null;

            switch ((ControllerTypes)inf.ReadInt32())
            {                
                case ControllerTypes.MSTSNotchController:
                    return new MSTSNotchController(inf);                

                case ControllerTypes.MSTSBrakeController:
                    return new MSTSBrakeController(inf);

                default:
					throw new InvalidDataException("Invalid controller type");
            }
        }
    }
}
