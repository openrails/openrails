using System.IO;
using Orts.Formats.Msts;

namespace Orts.Simulation.Signalling
{
    public class SpeedPostWorldObject
    {
        public string SFileName { get; }

        public SpeedPostWorldObject(SpeedPostObj speedPostItem)
        {
            // get filename in Uppercase
            SFileName = Path.GetFileName(speedPostItem.FileName).ToUpperInvariant();
        }

        public SpeedPostWorldObject(SpeedPostWorldObject other)
        {
            SFileName = other.SFileName;
        }
    }
}
