using System;
using System.IO;

namespace ORTS
{
    public enum ControllerTypes
    {
        SimpleController = 1,
        MSTSNotchController,
        MSTSStageController,
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
                case ControllerTypes.SimpleController:
                    return new SimpleController(inf);

                case ControllerTypes.MSTSNotchController:
                    return new MSTSNotchController(inf);

                case ControllerTypes.MSTSStageController:
                    return new MSTSStageController(inf);

                case ControllerTypes.MSTSBrakeController:
                    return new MSTSBrakeController(inf);

                default:
                    throw new Exception("Invalid controller type");
            }
        }
    }
}
