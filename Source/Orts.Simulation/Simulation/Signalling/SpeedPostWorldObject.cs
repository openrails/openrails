using Orts.Formats.Msts;
using System.IO;

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
    }
}
