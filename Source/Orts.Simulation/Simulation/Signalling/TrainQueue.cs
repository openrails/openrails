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
    public class TrainQueue : Queue<Train.TrainRouted>
    {
        /// <summary>
        /// Peek top train from queue
        /// </summary>
        public Train PeekTrain()
        {
            if (Count <= 0) return (null);
            Train.TrainRouted thisTrain = Peek();
            return (thisTrain.Train);
        }

        /// <summary>
        /// Check if queue contains routed train
        /// </summary>
        public bool ContainsTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain == null) return (false);
            return (Contains(thisTrain.Train.routedForward) || Contains(thisTrain.Train.routedBackward));
        }

        /// <summary>
        /// Dequeue the whole queue and re-enqueue the unmatched trains
        /// </summary>
        /// <param name="thisTrain"></param>
        public void RemoveTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain == null) return;
            var queueCount = Count; // loop through the original count to cover all trains in the queue
            for (var i = 0; i < queueCount; i++)
            {
                var queueTrain = Dequeue();
                if (queueTrain != null && queueTrain.Train != thisTrain.Train)
                    Enqueue(queueTrain);
            }
        }
    }
}
