using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public enum Direction { Forward, Reverse, N }

    public class DirectionControl
    {
        public static Direction Flip(Direction direction)
        {
            //return direction == Direction.Forward ? Direction.Reverse : Direction.Forward;
            if (direction == Direction.N)
                return Direction.N;
            if (direction == Direction.Forward)
                return Direction.Reverse;
            else
                return Direction.Forward;
        }
    }
}
