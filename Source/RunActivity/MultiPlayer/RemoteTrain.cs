// COPYRIGHT 2012, 2013 by the Open Rails project.
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

/* TRAINS
 * 
 * Contains code to represent a train as a list of TrainCars and to handle the physics of moving
 * the train through the Track Database.
 * 
 * A train has:
 *  - a list of TrainCars 
 *  - a front and back position in the TDB ( represented by TDBTravellers )
 *  - speed
 *  - MU signals that are relayed from player locomtive to other locomotives and cars such as:
 *      - direction
 *      - throttle percent
 *      - brake percent  ( TODO, this should be changed to brake pipe pressure )
 *      
 *  Individual TrainCars provide information on friction and motive force they are generating.
 *  This is consolidated by the train class into overall movement for the train.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;


namespace ORTS
{
	public class RemoteTrain :Train
	{
        public RemoteTrain(Simulator simulator):base(simulator)
        {
			TrainType = TRAINTYPE.REMOTE;
        }

		//update train location
		public override void Update(float elapsedClockSeconds)
		{
#if !NEW_SIGNALLING
			//if a MSGMove is received
			if (updateMSGReceived)
			{
				float move = 0.0f;
				try
				{
					var x = travelled + SpeedMpS * elapsedClockSeconds + (SpeedMpS - lastSpeedMps) / 2 * elapsedClockSeconds;

					if (Math.Abs(x - expectedTravelled) < 0.2 || Math.Abs(x - expectedTravelled) > 5) 
					{
						Traveller t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, Simulator.TDB.TrackDB.TrackNodes[expectedTracIndex], expectedTileX, expectedTileZ, expectedX, expectedZ, this.RearTDBTraveller.Direction);

						var y = this.travelled - expectedTravelled;
						this.travelled = expectedTravelled;
						this.RearTDBTraveller = t;
					}
					else//if the predicted location and reported location are similar, will try to increase/decrease the speed to bridge the gap in 1 second
					{
						SpeedMpS += (expectedTravelled - x) / 1;
						CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
					}
				}
				catch (Exception)
				{
					move = expectedTravelled - travelled;
				}
				CalculatePositionOfCars(move);
				updateMSGReceived = false;

#if false
				var x = travelled + SpeedMpS * elapsedClockSeconds + (SpeedMpS-lastSpeedMps)/2*elapsedClockSeconds;
				if (Math.Abs(x - expectedTravelled) < 0.2 || Math.Abs(x - expectedTravelled)>2)
				{
					CalculatePositionOfCars(expectedTravelled - travelled);
				}
				else
				{
					SpeedMpS += (expectedTravelled - x) / 1;
					CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
				}
				updateMSGReceived = false;
				if (this.RearTDBTraveller.TrackNodeIndex != expectedTracIndex)
				{
					try
					{
						Traveller t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, Simulator.TDB.TrackDB.TrackNodes[expectedTracIndex], expectedTileX, expectedTileZ, expectedX, expectedZ, this.RearTDBTraveller.Direction);
						this.RearTDBTraveller = t;
						CalculatePositionOfCars(0);
					}
					catch (Exception)
					{
					}
				}
#endif
			}
			else//no message received, will move at the previous speed
			{
				CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
			}
#endif
			//update speed for each car, so wheels will rotate
			foreach (TrainCar car in Cars)
			{
				if (car != null)
				{
					if (car.IsDriveable && car is MSTSWagon) (car as MSTSWagon).WheelSpeedMpS = SpeedMpS; 
					car.SpeedMpS = SpeedMpS;
				}
			}
#if !NEW_SIGNALLING
			lastSpeedMps = SpeedMpS;
#endif
		} // end Update
	}// class Train
}
