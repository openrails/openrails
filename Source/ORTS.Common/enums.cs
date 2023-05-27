// COPYRIGHT 2009, 2011 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

namespace ORTS.Common
{
    public enum Direction
    {
        [GetParticularString("Reverser", "Forward")] Forward,
        [GetParticularString("Reverser", "Reverse")] Reverse,
        [GetParticularString("Reverser", "N")] N
    }

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

    /// <summary>
    /// A type of horn pattern used by AI trains at level crossings.
    /// </summary>
    public enum LevelCrossingHornPattern
    {
        /// <summary>
        /// A single blast just before the crossing.
        /// </summary>
        Single,

        /// <summary>
        /// A long-long-short-long pattern used in the United States and Canada.
        /// </summary>
        US,
    }

    /// <summary>
    /// Defines the position of a load (e.g. a container) on a wagon
    /// </summary>
    public enum LoadPosition
    {
        Rear,
        CenterRear,
        Center,
        CenterFront,
        Front,
        Above
    }

    /// <summary>
    /// Defines the loading state of a load (e.g. a container) on a wagon
    /// </summary>
    public enum LoadState
    {
        Random,
        Empty,
        Loaded,
    }

}
