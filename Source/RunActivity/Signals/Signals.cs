using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace ORTS
{
    public class Signals
    {
        public Signals(Simulator simulator)
        {
        }

        // Restore state to resume a saved game
        public Signals(Simulator simulator, BinaryReader inf)
        {
        }

        // Save state to resume the game later
        public void Save(BinaryWriter outf)
        {
        }

        public void Update(float elapsedClockSeconds )
        {
        }
    }
}
