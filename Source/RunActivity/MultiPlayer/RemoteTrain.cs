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

/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

 
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
		public bool updateMSGReceived = false;
		public float expectedTravelled;
		public float lastSpeedMps = 0f;
		int expectedTileX, expectedTileZ, expectedTracIndex;
		float expectedX, expectedZ;

        public RemoteTrain(Simulator simulator):base(simulator)
        {
        }

		public void ToDoUpdate(int tni, int tX, int tZ, float x, float z, float eT, float speed)
		{
			SpeedMpS = speed;
			expectedTileX = tX;
			expectedTileZ = tZ;
			expectedX = x;
			expectedZ = z;
			expectedTravelled = eT;
			expectedTracIndex = tni;
			updateMSGReceived = true;
		}
		public override void Update(float elapsedClockSeconds)
		{
			if (updateMSGReceived)
			{
				float move = 0.0f;
				try
				{
					Traveller t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, Simulator.TDB.TrackDB.TrackNodes[expectedTracIndex], expectedTileX, expectedTileZ, expectedX, expectedZ, this.RearTDBTraveller.Direction);

					this.travelled = expectedTravelled;
					this.RearTDBTraveller = t;
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
			else
			{
				CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
			}

			lastSpeedMps = SpeedMpS;
		} // end Update
	}// class Train
}
