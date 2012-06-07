using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;

namespace ORTS.MultiPlayer
{
	public class OnlineTrains
	{
		public Dictionary<string, OnlinePlayer> Players;
		public OnlineTrains()
		{
			Players = new Dictionary<string, OnlinePlayer>();
		}
		public void Update()
		{

		}

		public Train findTrain(string name)
		{
			if (Players.ContainsKey(name))
				return Players[name].Train;
			else return null;
		}

		public bool findTrain(Train t)
		{

			foreach (OnlinePlayer o in Players.Values.ToList())
			{
				if (o.Train == t) return true;
			}
			return false;
		}

		public string MoveTrains(MSGMove move)
		{
			string tmp = "";
			if (move == null) move = new MSGMove();
			foreach (OnlinePlayer p in Players.Values)
			{
				if (p.Train != null && Math.Abs(p.Train.SpeedMpS) > 0.01)
				{
					move.AddNewItem(p.Username, p.Train.SpeedMpS, p.Train.travelled, p.Train.Number);
				}
			}
			foreach (Train t in Program.Simulator.Trains)
			{
				if (t == Program.Simulator.Trains[0]) continue;//player drived train
				if (findTrain(t)) continue;//is an online player controlled train
				if (t != null && Math.Abs(t.SpeedMpS) > 0.01)
				{
					move.AddNewItem("AI"+t.Number, t.SpeedMpS, t.travelled, t.Number);
				}
			}
			tmp += move.ToString();
			return tmp;

		}

		public string MoveAllPlayerTrain(MSGMove move)
		{
			string tmp = "";
			if (move == null) move = new MSGMove();
			foreach (OnlinePlayer p in Players.Values)
			{
				if (p.Train != null && Math.Abs(p.Train.SpeedMpS) > 0.01)
				{
					move.AddNewItem(p.Username, p.Train.SpeedMpS, p.Train.travelled, p.Train.Number);
				}
			}
			tmp += move.ToString();
			return tmp;
		}

		public string MoveAllTrain(MSGMove move)
		{
			string tmp = "";
			if (move == null) move = new MSGMove();
			foreach (Train t in Program.Simulator.Trains)
			{
				if (t != null && Math.Abs(t.SpeedMpS) > 0.01)
				{
					move.AddNewItem("AI"+t.Number, t.SpeedMpS, t.travelled, t.Number);
				}
			}
			tmp += move.ToString();
			return tmp;
		}

		public string AddAllPlayerTrain() //WARNING, need to change
		{
			string tmp = "";
			foreach (OnlinePlayer p in Players.Values)
			{
				if (p.Train != null)
				{
					MSGPlayer player = new MSGPlayer(p.Username, "1234", p.con, p.path, p.Train, p.Train.Number);
					tmp += player.ToString();
				}
			}
			return tmp;
		}
		public void AddPlayers(MSGPlayer player, OnlinePlayer p)
		{
			if (Players.ContainsKey(player.user)) return;
			if (Program.Client != null && player.user == Program.Client.UserName) return; //do not add self//WARNING: may need to worry about train number here
			if (p == null)
			{
				p = new OnlinePlayer(null, null);
			}
			p.con = Program.Simulator.BasePath + "\\TRAINS\\CONSISTS\\" + player.con;
			p.path = Program.Simulator.RoutePath + "\\PATHS\\" + player.path;
			p.Username = player.user;
			Players.Add(player.user, p);
			Train train = new RemoteTrain(Program.Simulator);
			if (MPManager.IsServer()) //server needs to worry about correct train number
			{
			}
			else
			{
				train.Number = player.num;
			}
			CONFile conFile;
			try
			{
				// This is the position of the back end of the train in the database.
				PATTraveller patTraveller = new PATTraveller(p.path);
				
				//set a rearTDBTraveller, init direction as forward
				train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, player.TileX, player.TileZ, player.X, player.Z, player.dir == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
				train.travelled = player.Travelled;
				// figure out if the next waypoint is forward or back
				//patTraveller.NextWaypoint();
				//if (train.RearTDBTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z) < 0)
				//	train.RearTDBTraveller.ReverseDirection();
				PATFile patFile = new PATFile(p.path);
				AIPath aiPath = new AIPath(patFile, Program.Simulator.TDB, Program.Simulator.TSectionDat, p.path);
				//aiPath.AlignAllSwitches();//WARNING
				conFile = new CONFile(p.con);
			}
			catch (Exception e)
			{
				if (MPManager.IsServer())
				{
					MPManager.BroadCast((new MSGMessage(player.user, "Error", "MultiPlayer Error："+e.Message)).ToString());
				}
				throw new MultiPlayerError();
			}

			// add wagons
			TrainCar previousCar = null;
			foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
			{

				string wagonFolder = Program.Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
				string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
				if (wagon.IsEngine)
					wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

				try
				{
					TrainCar car = ORTS.RollingStock.Load(Program.Simulator, wagonFilePath, previousCar);
					car.Flipped = wagon.Flip;
					car.UiD = wagon.UiD;
					car.CarID = player.user + " " + car.UiD; //player's train is always named train 0.
					train.Cars.Add(car);
					car.Train = train;
					previousCar = car;
					MSTSWagon w = (MSTSWagon)car;
					if (w != null)
					{
						w.AftPanUp = player.pantofirst == 1 ? true : false;
						w.FrontPanUp = player.pantosecond == 1 ? true : false;
					}

				}
				catch (Exception)
				{
				}


			}// for each rail car

			if (train.Cars.Count == 0) return;

			if (train.Cars[0] is MSTSLocomotive) train.LeadLocomotive = train.Cars[0];

			//train.Number = player.num;
			train.CalculatePositionOfCars(0);
			p.Train = train;
			Program.Simulator.Trains.Add(train);
			if (MPManager.IsServer())
			{
				train.InitializeSignals(false);
			}

		}
	}
}
