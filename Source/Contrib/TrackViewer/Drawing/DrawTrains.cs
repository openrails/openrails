// COPYRIGHT 2014 by the Open Rails project.
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
//
// Draw trains running in ORTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS.Common;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This class is intended to be a link between ORTS and trackviewer. The intention was to make it possible
    /// to show the actual location of the player and other trains on the map.
    /// However, that kind of functionality is already available in the dispatcher window in ORTS.
    /// So, perhaps this class should be removed.
    /// </summary>
    public class DrawTrains
    {
        WorldLocation trainLocation;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DrawTrains()
        {
        }

        /// <summary>
        /// Update the train location from RunActivity and shift the drawArea to its location (if found)
        /// </summary>
        /// <param name="drawArea"></param>
        /// <returns>Whether an updated location could be found.</returns>
        public bool Update(DrawArea drawArea)
        {
            // Get location from RunActivity
            trainLocation = ORTS.Processes.TrackviewerIpc.PlayerTrainTraveller();
            if (trainLocation != WorldLocation.None)
            {
                drawArea.ShiftToLocation(trainLocation);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draw a train at location of the player train
        /// </summary>
        /// <param name="drawArea">The drawarea to draw upon</param>
        public void Draw(DrawArea drawArea)
        {
            if (trainLocation != WorldLocation.None)
            {
                float size = 9f; // in meters
                int minPixelSize = 7;
                drawArea.DrawTexture(trainLocation, "playerTrain", size, minPixelSize);
            }
        }
    }
}
