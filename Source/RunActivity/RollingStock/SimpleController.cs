using MSTS;
using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ORTS
{
    /**
     * This class is used as a default controller implementation. 
     * This simples interpolate a value between 0 and 100.               
     */
    class SimpleController: IController
    {
        public const float INC_VALUE = 10;

        private float ThrottlePercent;

        public SimpleController()
        {
        }

        public SimpleController(BinaryReader inf)
        {
            this.RestoreData(inf);
        }

        public SimpleController(SimpleController other)
        {
            ThrottlePercent = other.ThrottlePercent;
        }

        public IController Clone()
        {
            return new SimpleController(this);
        }

        public void Parse(STFReader f)
        {
            throw new NotImplementedException("Parse not suported by SimpleController");
        }

        public string GetStatus()
        {
            return string.Format("{0:F0}%", ThrottlePercent);
        }

        private void ChangeThrottlePercent(float value)
        {
            ThrottlePercent = MathHelper.Clamp(ThrottlePercent + value, 0, 100);
        }

        public float Increase(float elapsedSeconds)
        {
            ChangeThrottlePercent(INC_VALUE * elapsedSeconds);

            return ThrottlePercent / 100.0f;
        }

        public float Decrease(float elapsedSeconds)
        {
            ChangeThrottlePercent(-INC_VALUE * elapsedSeconds);

            return ThrottlePercent / 100.0f;
        }

        public bool IsNotched()
        {
            return false;
        }

        public bool IsValid()
        {
            return true;
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.SimpleController);

            this.SaveData(outf);
        }

        protected void SaveData(BinaryWriter outf)
        {
            outf.Write(ThrottlePercent);
        }

        private void RestoreData(BinaryReader inf)
        {
            ThrottlePercent = inf.ReadSingle();
        }
    }
}
