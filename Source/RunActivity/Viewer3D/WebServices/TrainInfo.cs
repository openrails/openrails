// COPYRIGHT 2021 by the Open Rails project.
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

using System;
using static Orts.Simulation.Physics.Train;

namespace Orts.Viewer3D.WebServices
{
    /// <summary>
    /// Contains information on the current player train status.
    /// </summary>
    public struct TrainInfo
    {
        /// <summary>
        /// The current control mode of the player train.
        /// </summary>
        /// <remarks>
        /// Value is the string equivalent to a <see cref="TRAIN_CONTROL"/> value.
        /// </remarks>
        public string ControlMode;
    }

    public static class TrainInfoExtensions
    {
        /// <summary>
        /// Get the player train status.
        /// </summary>
        /// <param name="viewer">The Viewer3D instance.</param>
        /// <returns></returns>
        public static TrainInfo GetWebTrainInfo(this Viewer viewer) => new TrainInfo
        {
            ControlMode = Enum.GetName(typeof(TRAIN_CONTROL), viewer.PlayerTrain.ControlMode),
        };
    }
}
