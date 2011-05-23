// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

/* RoadCars
 * 
 * Contains code to represent a train as a list of RoadCarCars and to handle the physics of moving
 * the car through the RDB Database.
 * 
 * CarSpawner: loaded when the WFile is in, it will find the origin of the spawner, place several cars on the road,
 *             find crossings and tracks, and also train track may be higher than road track, it will try to compute that
 *             The spawner will be updated when a corresponding crossing is loaded by WFile
 * 
 * RoadCar: the car with a shape randomly picked, it will move along the road and slow down/stop is a gate is closed
 * 
 * RoadCarDrawer: holds the list of cars currently on the road and update them. This is necessary since a spawner may be
 *             unloaded but cars on the road may not
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;


namespace ORTS
{

	public class CarSpawner
	{
		bool silent = false;
		public int direction;
		public float CarFrequency;
		public float CarAvSpeed;
		public WorldPosition position;
		public float lastSpawnedTime;
		public float nextSpawnTime; //time elapsed till next spawn

		private float StartData1;//the first of SData of the start location
		private float EndData1; //the first of SData of the end location
		public float roadLength; // how far away between the start and end, use to reduce computing

		public float X, Y, Z; //start point information
		public int TileX, TileZ;
		
		public int TileX2, TileZ2; //end point information
		public float X2, Y2, Z2;

		public RoadCar lastCar; //last car spawned

		//crossing interacted with this spawner
		public int numCrossings;
		public float[] crossingDistanceChart; //how far away crossings are from the start
		public LevelCrossingObject[] crossingObjects; //the crossing objects
		SortedList<float, float> listOfTracks;
		float[] trackRange; //several track may be close, so they form a track range
		float[] trackHeight; //tracks may be higher than road, this will store the difference
		int trackNum;

		public float topY;

		public CarSpawner(CarSpawnerObj spawnerObj, WorldPosition wp)
		{

			int start, end;

			start = spawnerObj.getTrItemID(0);
			end = spawnerObj.getTrItemID(1);
			if (spawnerObj.CarFrequency < 0.1 || spawnerObj.CarAvSpeed < 0.1 || start < 0 || end < 0) // in case data is wrong
			{
				silent = true;
				return;
			}

			lastCar = null;

			CarFrequency = spawnerObj.CarFrequency; //average frequency
			CarAvSpeed =  spawnerObj.CarAvSpeed; //the final speed will be on 3/4 - 1 1/4


			//set start tile etc
			TrItem[] trTable = Program.Simulator.RDB.RoadTrackDB.TrItemTable;
			TileX = trTable[start].TileX;
			TileZ = trTable[start].TileZ;
			X = trTable[start].X;

			Y = trTable[start].Y;
			Z = trTable[start].Z;

			// set end tile etc
			TileX2 = trTable[end].TileX;
			TileZ2 = trTable[end].TileZ;
			X2 = trTable[end].X;
			Y2 = trTable[end].Y;
			Z2 = trTable[end].Z;

			StartData1 = trTable[start].SData1; //SData1 of the spawner
			EndData1 = trTable[end].SData1;

			lastSpawnedTime = 0;
			nextSpawnTime = CarFrequency * (0.75f + (float)Program.Random.NextDouble() / 2);

			//test the road, 1. direction should be 1; 2. dist to the end
			direction = 1;
			RDBTraveller CarRDBTraveller;
			try
			{
				CarRDBTraveller = new RDBTraveller(TileX, TileZ, X, Z, direction, Program.Simulator.RDB, Program.Simulator.TSectionDat);
				if (CarRDBTraveller.MoveTo(TileX2, TileZ2, X2, Y2, Z2) != true) //cannot reach the end, so the direction is wrong
				{
					direction = 0;
				}
			}
			catch (Exception e)
			{
				silent = true;
				return;
			}

			//dist to the end, need to reset the RDB traveller
			CarRDBTraveller = new RDBTraveller(TileX, TileZ, X, Z, direction, Program.Simulator.RDB, Program.Simulator.TSectionDat);
			roadLength = CarRDBTraveller.DistanceTo(TileX2, TileZ2, X2, Y2, Z2) - 1.0f; //-1.0f to be a bit safe to incorrprate numerical error

			//shoot some cars to the road so that the road is populated first
			RoadCar temp;
			SortedList<float, RoadCar> listOfCar = new SortedList<float, RoadCar>();
			float dist;
			int i;

			var initSeconds = 120; //populate the road with 120s of time, i.e., try to create 120/CarFrequency cars.

			for (i = 0; i < initSeconds; i++)
			{
				temp = SpawnCars(1);
				if (temp == null) continue;
				dist = (initSeconds - i) * temp.SpeedMpS; // the car travelled this amount of dist
				temp.Move(dist);
				try
				{
					if (temp.outOfRoad == false) listOfCar.Add(-temp.travelledDist, temp);//add to the list
				}
				catch (Exception e) { }

			}
			
			//take care of previous car of each car
			if (listOfCar.Count > 0)
			{
				listOfCar.ElementAt(0).Value.previous = null;
				for (i = 1; i < listOfCar.Count; i++)
				{
					listOfCar.ElementAt(i).Value.previous = listOfCar.ElementAt(i - 1).Value;
				}
				this.lastCar = listOfCar.Last().Value;
			}
			else lastCar = null;

			//check for crossings it will interact
			numCrossings = 0;
			trackNum = 0; //number of tracks it will interact with

			if (Program.Simulator.LevelCrossings == null) return; //no crossing, or crossing is not populated yet
			LevelCrossingObject crossingObj;

			int crSize = Program.Simulator.LevelCrossings.noCrossing;
			SortedList<float, LevelCrossingObject> listOfCrossing = new SortedList<float, LevelCrossingObject>();
			List<LevelCrossingObject> added = new List<LevelCrossingObject>(); //crossings being added, including sisters;
			for (i = 0; i < crSize; i++) //check to find all crossings
			{
				crossingObj = Program.Simulator.LevelCrossings.LevelCrossingObjects[i];
				if (crossingObj == null) continue;
				if (added.Contains(crossingObj)) continue;
				dist = crossingObj.DistanceTo(CarRDBTraveller);
				if (dist < 0) continue;
				try
				{
					listOfCrossing.Add(dist, crossingObj);
					crossingObj.carSpawner = this;
				}
				catch (Exception e) { }
			}

			//the road has crossings, build the distance chart and crossings
			if (listOfCrossing.Count > 0)
			{
				numCrossings = listOfCrossing.Count;
				crossingDistanceChart = new float[numCrossings];
				crossingObjects = new LevelCrossingObject[numCrossings];
				for (i = 0; i < numCrossings; i++)
				{
					crossingDistanceChart[i] = listOfCrossing.ElementAt(i).Key;
					crossingObjects[i] = listOfCrossing.ElementAt(i).Value;
				}
			}
		}//constructor

		public void CheckGatesAgain(LevelCrossingObject crossingObj) //needed since gate may be loaded later
		{
			if (numCrossings <= 0) return;

			float key;
			int loc;
			for (loc = 0; loc < numCrossings; loc++)
			{
				if (crossingObjects[loc] == crossingObj) break;
			}

			if (loc >= numCrossings) return;// did not find the crossing

			SortedList<float, LevelCrossingObject> listOfCrossing = new SortedList<float, LevelCrossingObject>();
			listOfTracks = new SortedList<float, float>();

			for (int i = 0; i < numCrossings; i++)
			{
				//only push those not affected in
				if (crossingObjects[i] != crossingObj)
					listOfCrossing.Add(crossingDistanceChart[i], crossingObjects[i]);
			}

			key = crossingDistanceChart[loc];

			TrItem[] trTable = Program.Simulator.RDB.RoadTrackDB.TrItemTable;
			TrItem[] trTable2 = Program.Simulator.TDB.TrackDB.TrItemTable;


			float dist;
			int RDBID2 = -1;
			foreach (LevelCrossingObject sister in crossingObj.groups)
			{
				int current = 0;
				int RDBId = sister.levelCrossingObj.getTrItemID(current, 1);
				while (RDBId >= 0)
				{
					//find the dist, no need to compute, just poll
					dist = Math.Abs(StartData1 - trTable[RDBId].SData1);
					RDBID2 = sister.levelCrossingObj.getTrItemID(current, 0);

					if (RDBID2 >= 0)
					{
						dist = CheckTrackHeight(trTable, RDBId, trTable2, RDBID2, dist);
						if (dist < key) key = dist;
					}
					current++;
					RDBId = sister.levelCrossingObj.getTrItemID(current, 1);
				}

			}
			trackNum = 0;
			try
			{
				//build the database for tracks and its height, if two tracks are close, they are considered as one unit
				trackRange = new float[(listOfTracks.Count + 1) * 2]; 
				trackHeight = new float[listOfTracks.Count + 1];
				int i = 0, k = 0;
				trackRange[i] = listOfTracks.First().Key - 2;//road bed starts 2 meters on the left of the track
				trackHeight[i / 2] = listOfTracks.First().Value;
				trackNum = 1;
				for (k = 1; k < listOfTracks.Count; k++)
				{
					//if two tracks are close (< 7 meters), they are considered as one track
					if (listOfTracks.ElementAt(k).Key - listOfTracks.ElementAt(k - 1).Key > 7) //two tracks are far away (>7 meters), thus may not consider continued road
					{
						trackRange[++i] = listOfTracks.ElementAt(k - 1).Key + 2; //road bed starts 2 meters on the left of the track
						trackRange[++i] = listOfTracks.ElementAt(k).Key - 2; //road bed ends 2 meters on the right of the track
						trackNum++;
					}
					trackHeight[i / 2] = listOfTracks.First().Value;
				}
				trackRange[++i] = listOfTracks.ElementAt(k - 1).Key + 2;//road bed ends 2 meters on the right of the track
				listOfCrossing.Add(key, crossingObj);
			}
			catch (Exception e) {
			}

			//rebuild the map of gates/dist chart
			if (listOfCrossing.Count > 0)
			{
				numCrossings = listOfCrossing.Count;
				crossingDistanceChart = new float[numCrossings];
				crossingObjects = new LevelCrossingObject[numCrossings];
				for (int i = 0; i < numCrossings; i++)
				{
					crossingDistanceChart[i] = listOfCrossing.ElementAt(i).Key;
					crossingObjects[i] = listOfCrossing.ElementAt(i).Value;
				}
			}
		}

		public float CheckTrackHeight(TrItem[] trTable, int RDBId, TrItem[] trTable2, int RDBId2, float dist)
		{
			float height;
			try//find the height difference of track and road
			{
				int Dir = 0;
				if (direction == 0) Dir = 1;
				//road height is determined by the car traveller on the road
				RDBTraveller carTraveller = new RDBTraveller(trTable[RDBId].TileX, trTable[RDBId].TileZ, trTable[RDBId].X, trTable[RDBId].Z, Dir, Program.Simulator.RDB, Program.Simulator.TSectionDat);
				//track height is determined by the train traveller on the track
				TDBTraveller trainTraveller = new TDBTraveller(trTable2[RDBId2].TileX, trTable2[RDBId2].TileZ, trTable2[RDBId2].X, trTable2[RDBId2].Z, 1, Program.Simulator.TDB, Program.Simulator.TSectionDat);
				height = trainTraveller.Y + 0.275f - carTraveller.Y;

				carTraveller.Direction = 0; //reverse to go to the origin
				float temp = carTraveller.DistanceTo(TileX, TileZ, X, Y, Z);
				//if the temp is -1, it means the item is not on the road, thus the data need to be curated.
				if (temp == -1)
				{
					TDBTraveller copy = new TDBTraveller(trainTraveller);
					float tried = 0;
					while (tried < 100)//move forward to test 100 times (50 meters)
					{
						copy.Move(0.5f);
						tried += 0.5f;
						try
						{
							carTraveller = new RDBTraveller(copy.TileX, copy.TileZ, copy.X, copy.Z, Dir, Program.Simulator.RDB, Program.Simulator.TSectionDat);
							temp = carTraveller.DistanceTo(TileX, TileZ, X, Y, Z);
							if (temp > 0)
							{
								height = copy.Y + 0.275f - carTraveller.Y;
								break;
							}
						}
						catch (Exception e)
						{
						}
					}
					tried = 0;
					copy = new TDBTraveller(trainTraveller);
					while (temp < 0 && tried < 100)//move backward to test 100 times (50 meters)
					{
						copy.Move(-0.5f);
						tried += 0.5f;
						try
						{
							carTraveller = new RDBTraveller(copy.TileX, copy.TileZ, copy.X, copy.Z, Dir, Program.Simulator.RDB, Program.Simulator.TSectionDat);
							temp = carTraveller.DistanceTo(TileX, TileZ, X, Y, Z);
							if (temp > 0)
							{
								height = copy.Y + 0.275f - carTraveller.Y;
								break;
							}
						}
						catch (Exception e)
						{
						}
					}
					if (temp >= 0 && Math.Abs(temp - dist) > 1)
					{
						dist = temp; //if the data is curated with significant diff, use the new data
					}
				}
				listOfTracks.Add(dist, height > 0 && height < 1 ? height : 0);//if track is too high, ignore as well

			}
			catch (Exception e)
			{
			}
			return dist;
		}

		public float CheckTrack(float travelledDist)
		{
			float top = 0;
			if (trackNum == 0) return top;

			//most time will be either not reaching a track, or has left the tracks
			if (travelledDist < trackRange[0] || travelledDist > trackRange[2 * trackNum - 1]) return top;

			for (int i = 0; i < trackNum; i++)
			{
				if (trackRange[2 * i] <= travelledDist && travelledDist <= trackRange[2 * i + 1])
				{
					top = trackHeight[i];
					break;
				}
			}
			return top;
		}

		//add a car to the road if possible: 1) the previous car has moved out, 2) ready to spawn based on the frequency
		public RoadCar SpawnCars(float clock)
		{
			//something wrong of the data 
			if (silent == true) return null;
			lastSpawnedTime += clock;
			if (lastCar != null && lastCar.travelledDist < 20) return null; //traffic jam
			if (lastSpawnedTime < nextSpawnTime) return null; // not yet to spawn a car
			lastSpawnedTime = 0;

			RoadCar temp = new RoadCar(this);
			nextSpawnTime = CarFrequency * (0.75f + (float)Program.Random.NextDouble() / 2); //between 3/4-1 1 1/4 of the set up
			if (temp.outOfRoad == true) temp = null; //cannot add car shape (wrong with carspawn.dat), thus return nothing
			return temp;
		}
	}

	/// <summary>
	/// Class to handle car movement and interaction with crossings
	/// </summary>
	public class RoadCar
	{
		public RDBTraveller CarRDBTraveller;   // position of the car
		public float SpeedMpS = 0.0f;  // meters per second +ve forward, -ve when backing
		public RoadCarShape carShape;
		public CarSpawner spawner;
		public bool outOfRoad = false;
		public RoadCar previous; //car in front of
		public float travelledDist; //how far from the origin, no need to compute from DistanceTo, added every frame
		private int crossingGate = 0;// which is the first crossing it will cross
		public float safeDist; //keep safe distance from
		private float desiredSpeed; //max and desired speed
		public float carWheelPos;

		/// <summary>
		/// Move the car along the road, rotate the car based on its front/end vector, and move the car higher if the rail
		/// track is higher
		/// </summary>
		public void Move(float dist) //move along the road some distance
		{
			//add to the travelled dist
			travelledDist += dist; //add to the travelled dist

			if (travelledDist > spawner.roadLength)//check if out of road
			{
				outOfRoad = true;
				return;
			}

			//move the traveller
			CarRDBTraveller.Move(dist);
			carShape.movablePosition.TileX = CarRDBTraveller.TileX;
			carShape.movablePosition.TileZ = CarRDBTraveller.TileZ;

			float added1 = 0.0f; //track height added for front wheel
			float added2 = 0.0f; //track height added for back wheel

			added1 = spawner.CheckTrack(travelledDist + carWheelPos);
			added2 = spawner.CheckTrack(travelledDist - carWheelPos);

			//we need to compute the rotation matrix, so use the front and end of the car
			RDBTraveller copy1 = new RDBTraveller(CarRDBTraveller);
			RDBTraveller copy2 = new RDBTraveller(CarRDBTraveller);
			copy1.Move(carWheelPos); //move front wheel to its position
			copy2.Move(-carWheelPos); //move back wheel to its position

			//compute the rotation matrix from front and back wheel
			carShape.movablePosition.XNAMatrix = Matrix.Identity;
			carShape.movablePosition.XNAMatrix *= RotationMatrixFromMSTSCoordinates(
				copy2.X, copy2.Y + 0.1f + added2, copy2.Z, copy1.X, copy1.Y + 0.1f + added1, copy1.Z,
				copy2.TileX, copy2.TileZ, copy1.TileX, copy1.TileZ); 
			//MSTS car looks be running a bit lower, thus +0.1 to bring the wheel above road

			carShape.movablePosition.XNAMatrix *= Matrix.CreateTranslation(CarRDBTraveller.X,
				(copy2.Y + added2 + copy1.Y + added1) / 2 + 0.1f, -CarRDBTraveller.Z); //create the location
			     // the y location is the half of the sum of end and front
		}

		/// <summary>
		/// create a car and put it on the road
		/// </summary>
		public RoadCar(CarSpawner r)
		{
			//assign the previous car
			previous = r.lastCar; //assign the last car spawned as my previous
			r.lastCar = this; //I am the last car now

			//init location and speed
			CarRDBTraveller = new RDBTraveller(r.TileX, r.TileZ, r.X, r.Z, r.direction, Program.Simulator.RDB, Program.Simulator.TSectionDat);
			spawner = r;
			desiredSpeed = SpeedMpS = r.CarAvSpeed * (0.75f + (float) Program.Random.NextDouble() / 2);
			travelledDist = 0.0f;
			RoadCarHandler.Viewer.RoadCarHandler.AddCarShape(this); // add car shape
		}

		//update the car, first speed, then move
		public void Update(float elapsedClockSeconds)
		{
			if (outOfRoad == true) return; //out of road, do nothing


			float distToCar, distToGate=-1;

			//check which crossing should it interact with
			if (crossingGate < spawner.numCrossings)
			{
				distToGate = spawner.crossingDistanceChart[crossingGate] - travelledDist;
				while (distToGate < 0)
				{
					crossingGate++; //move to the next gate area
					if (crossingGate >= spawner.numCrossings) break;
					distToGate = spawner.crossingDistanceChart[crossingGate] - travelledDist;
				}
			}

			//clean the assignment of previous car
			if (previous != null && previous.outOfRoad == true) previous = null;
			if (previous == null) //no car in front of me, I will travel as needed
			{
				if (SpeedMpS < desiredSpeed - 0.1) //I am slower than expect, will graduately accelarate to the speed
				{
					SpeedMpS += (desiredSpeed - SpeedMpS) * elapsedClockSeconds / 3.0f;
				}
				distToCar = spawner.roadLength;
			}
			else
			{
				distToCar = previous.travelledDist - travelledDist;
			}

			//we have gate, check with the next gate if any
			if (crossingGate < spawner.numCrossings && spawner.crossingObjects[crossingGate].HasTrain())
			{
				distToGate = spawner.crossingDistanceChart[crossingGate] - travelledDist;
				if (distToGate < 5) //may just rush
				{
					SpeedMpS += (desiredSpeed - SpeedMpS) / 2;
				}
				else if (distToGate < 10 + safeDist / 2)//stop immediately if I am close
				{
					SpeedMpS = 0;
					return;
				}
				else if (distToGate < 29.9f && distToCar > 50) // slow down a bit
				{
					SpeedMpS = desiredSpeed * (0.8f - (20.0f - distToGate) / 20.0f);
				}
			}

			//car in front of me too close
			if (previous != null && distToCar < (previous.safeDist + safeDist) / 2.0f)
			{
				SpeedMpS = previous.SpeedMpS;
			}
			else //distance OK, need to see if need to slow down
			{
				if (distToCar < 20)
				{
					//front is faster, so will increase speed, otherwise, slow a bit
					if (previous != null && SpeedMpS < previous.SpeedMpS) SpeedMpS -= (SpeedMpS - previous.SpeedMpS) * elapsedClockSeconds / 2.0f;
					else SpeedMpS = desiredSpeed * (0.2f - 0.1f * (20.0f - distToCar) / 20.0f);
				}
				else if (distToCar < 40)
				{
					//front is faster, so will increase speed, otherwise, slow a bit
					if (previous != null && SpeedMpS < previous.SpeedMpS) SpeedMpS -= (SpeedMpS - previous.SpeedMpS) * elapsedClockSeconds / 2.0f;
					else SpeedMpS = desiredSpeed * (1.0f - (45.0f - distToCar) / 50.0f);
				}
				else
				{
					if (SpeedMpS < 2 && distToCar > 50)
					{
						SpeedMpS += 0.2f; // start slow
					}
					else SpeedMpS += (desiredSpeed - SpeedMpS) / 10.0f;
				}

			}

			//sanity checks
			if (SpeedMpS < 0.01)
			{
				SpeedMpS = 0.0f;
				return;
			}
			if (SpeedMpS > desiredSpeed - 1) SpeedMpS = desiredSpeed;

			//now move the car
			Move(elapsedClockSeconds * SpeedMpS);//move a distance

			//move out of road?
			if (travelledDist > spawner.roadLength) // out of road
			{
				outOfRoad = true;
			}
		} // end Update

		/// <summary>
		/// The front end of a railcar is at MSTS world coordinates x1,y1,z1, TileX1, TileZ1
		/// The other end is at x2,y2,z2, TileX2, TileZ2
		/// Return a rotation (not translation) matrix for the center of the railcar.
		/// </summary>
		public static Matrix RotationMatrixFromMSTSCoordinates(float x1, float y1, float z1, float x2, float y2, float z2, int TileX1, int TileZ1, int TileX2, int TileZ2)
		{
			// translate 1st coordinate to be relative to 0,0,0
			float dx = (float)((TileX1 - TileX2) * 2048 + x1 - x2);
			float dy = (float)(y1 - y2);
			float dz = (float)((TileZ1 - TileZ2) * 2048 + z1 - z2);

			// compute the rotational matrix  
			float length = (float)Math.Sqrt(dx * dx + dz * dz + dy * dy);
			float run = (float)Math.Sqrt(dx * dx + dz * dz);
			// normalize to coordinate to a length of one, ie dx is change in x for a run of 1
			dx /= length;
			dy /= length;   // ie if it is tilted back 5 degrees, this is sin 5 = 0.087
			run /= length;  //                              and   this is cos 5 = 0.996
			dz /= length;
			// setup matrix values

			Matrix xnaTilt = new Matrix(1, 0, 0, 0,
									 0, run, dy, 0,
									 0, -dy, run, 0,
									 0, 0, 0, 1);

			Matrix xnaRotation = new Matrix(dz, 0, dx, 0,
											0, 1, 0, 0,
											-dx, 0, dz, 0,
											0, 0, 0, 1);

			return xnaTilt * xnaRotation;
		}
	}// class RoadCar

	/// <summary>
	/// The class hold all cars and information of the viewer/simulator
	/// </summary>
	public class RoadCarHandler
	{
		private RoadCar[] cars;
		private int capacity; //how many cars can hold
		private int numCars; //how many cars now
		public static Viewer3D Viewer;
		/// <summary>
		/// Cars will be loaded into this viewer.
		/// </summary>
		public RoadCarHandler(Viewer3D viewer)
		{
			Viewer = viewer;
			capacity = 128;
			numCars = 0;
			cars = new RoadCar[capacity]; //initial to take 128 cars most, but will grow if needed
		}

		/// <summary>
		/// add shape to a car, randomly pick a shape, and assign its location/rotation based on traveller
		/// </summary>
		public void AddCarShape(RoadCar r)
		{
			if (Program.Simulator.CarSpawnerFile.shapeNames.Length <= 0) //error in carspawn.dat
			{
				r.outOfRoad = true;
				return;
			}
			//randomly pick a shape and its safe distance
			int i = Program.Random.Next() % Program.Simulator.CarSpawnerFile.shapeNames.Length;
			string shapeFilePath = Program.Simulator.CarSpawnerFile.shapeNames[i]; //shape directory has been added into the filename, so no need to worry
			r.safeDist = Program.Simulator.CarSpawnerFile.distanceFrom[i];
			r.carWheelPos = r.safeDist / 3; //wheels are safeDist/3 from the center

			//find the location/rotation from the traveller
			WorldLocation wl = new WorldLocation(r.CarRDBTraveller.WorldLocation);
			wl.Location.Z *= -1;
			WorldPosition w = new WorldPosition();
			w.TileX = r.CarRDBTraveller.WorldLocation.TileX;
			w.TileZ = r.CarRDBTraveller.WorldLocation.TileZ;
			w.XNAMatrix = Matrix.CreateTranslation(wl.Location);
			r.carShape = new RoadCarShape(Viewer, shapeFilePath, w);

			r.Move(r.carWheelPos); //move the car forward so the shape rotation matrix can be corrected and back wheel will be on the road
			if (r.outOfRoad == true) return; //check if out of road, just in case

			//too many cars, grow the car array
			if (numCars >= capacity - 1) //array too small, grow
			{
				capacity += 128; //grow by 128
				RoadCar[] tmp = new RoadCar[capacity];
				for (i = 0; i < numCars; i++)
				{
					tmp[i] = cars[i]; //copy from old to new
				}
				cars = tmp; //finished
			}
			//add the car into the car array
			cars[numCars++] = r;
			return;
		}

		/// <summary>
		/// Loop through cars to update them and animate them
		/// </summary>
		public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
		{
			RoadCar car;
			//loop through all cars
			for (int i = numCars - 1; i >= 0; i--)
			{
				car = cars[i];

				car.Update(elapsedTime.ClockSeconds);
				//if car is out, remove it (copy the last car to occupy its position in the car array
				if (car.outOfRoad == true)
				{
					cars[i] = cars[numCars - 1]; //car is out, copy the last car to occupy the position
					numCars--;
					continue;
				}
				car.carShape.PrepareFrame(frame, elapsedTime);//animate it
			}
		}
	} // RoadCarDrawer


}
