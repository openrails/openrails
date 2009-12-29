using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public enum Direction { Forward, Reverse }

    public class DirectionControl
    {
        public static Direction Flip(Direction direction)
        {
            return direction == Direction.Forward ? Direction.Reverse : Direction.Forward;
        }
    }
}
