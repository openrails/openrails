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

using Orts.Simulation.Physics;
using System.Collections.Generic;

namespace Orts.Simulation.Signalling
{
    public class TrainOccupyState : Dictionary<Train.TrainRouted, int>
    {
        /// <summary>
        /// Check if it contains specified train
        /// Routed
        /// </summary>
        public bool ContainsTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain == null) return (false);
            return (ContainsKey(thisTrain.Train.routedForward) || ContainsKey(thisTrain.Train.routedBackward));
        }

        /// <summary>
        /// Check if it contains specified train
        /// Unrouted
        /// </summary>
        public bool ContainsTrain(Train thisTrain)
        {
            if (thisTrain == null) return (false);
            return (ContainsKey(thisTrain.routedForward) || ContainsKey(thisTrain.routedBackward));
        }

        /// <summary>
        /// Check if it contains specified train and return train direction
        /// Routed
        /// </summary>
        public Dictionary<bool, int> ContainsTrainDirected(Train.TrainRouted thisTrain)
        {
            Dictionary<bool, int> returnValue = new Dictionary<bool, int>();
            return (ContainsTrainDirected(thisTrain.Train));
        }

        /// <summary>
        /// Check if it contains specified train and return train direction
        /// Unrouted
        /// </summary>
        public Dictionary<bool, int> ContainsTrainDirected(Train thisTrain)
        {
            Dictionary<bool, int> returnValue = new Dictionary<bool, int>();
            bool trainFound = false;

            if (thisTrain != null)
            {
                trainFound = ContainsKey(thisTrain.routedForward) || ContainsKey(thisTrain.routedBackward);
            }

            if (!trainFound)
            {
                returnValue.Add(false, 0);
            }
            else
            {
                int trainDirection = 0;
                if (ContainsKey(thisTrain.routedForward))
                {
                    trainDirection = this[thisTrain.routedForward];
                }
                else
                {
                    trainDirection = this[thisTrain.routedBackward];
                }

                returnValue.Add(true, trainDirection);
            }
            return (returnValue);
        }

        /// <summary>
        /// Remove train from list
        /// Routed
        /// </summary>
        public void RemoveTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain != null)
            {
                if (ContainsTrain(thisTrain.Train.routedForward)) Remove(thisTrain.Train.routedForward);
                if (ContainsTrain(thisTrain.Train.routedBackward)) Remove(thisTrain.Train.routedBackward);
            }
        }
    }
}
