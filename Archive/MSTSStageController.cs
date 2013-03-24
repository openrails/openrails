using MSTS;
using System;
using System.IO;

namespace ORTS
{
    /**
     *  This almost the same as the Notch controller, but it can be updated
     *  every frame it updates its values continuosoly, not only on Notches.
     *  Useful for dynamic brakes and steam locomotives regulators
     */
    class MSTSStageController: MSTSNotchController
    {
        public MSTSStageController()
        {
        }

        public MSTSStageController(MSTSStageController other):
            base(other)
        {
            
        }

        public MSTSStageController(STFReader f):
            base(f)               
        {            
        }

        public MSTSStageController(BinaryReader inf) :
            base(inf)
        {

        }

        public override IController Clone()
        {
            return new MSTSStageController(this);
        }

        public override bool IsNotched()
        {
            return Notches.Count > 0 && !Notches[CurrentNotch].Smooth;
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.MSTSStageController);

            this.SaveData(outf);
        }        
    }
}
