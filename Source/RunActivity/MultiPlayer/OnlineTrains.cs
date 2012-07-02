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
				if (p.Train != null && Program.Simulator.PlayerLocomotive != null && p.Train != Program.Simulator.PlayerLocomotive.Train)
				{
					if (Math.Abs(p.Train.SpeedMpS) > 0.001 || Math.Abs(p.Train.LastReportedSpeed) > 0)
					{
						move.AddNewItem(p.Username, p.Train);
					}
				}
			}
			foreach (Train t in Program.Simulator.Trains)
			{
				if (Program.Simulator.PlayerLocomotive != null && t == Program.Simulator.PlayerLocomotive.Train) continue;//player drived train
				if (t == null || findTrain(t)) continue;//is an online player controlled train
				if (Math.Abs(t.SpeedMpS) > 0.001 || Math.Abs(t.LastReportedSpeed) > 0)
				{
					move.AddNewItem("0xAI"+t.Number, t);
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
				if (p.Train == null) continue;
				if (Math.Abs(p.Train.SpeedMpS) > 0.001 || Math.Abs(p.Train.LastReportedSpeed) > 0)
				{
					move.AddNewItem(p.Username, p.Train);
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
				if (t != null && (Math.Abs(t.SpeedMpS) > 0.001 || Math.Abs(t.LastReportedSpeed) > 0))
				{
					move.AddNewItem("AI"+t.Number, t);
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
			p.LeadingLocomotiveID = player.leadingID;
			Players.Add(player.user, p);
			p.con = Program.Simulator.BasePath + "\\TRAINS\\CONSISTS\\" + player.con;
			p.path = Program.Simulator.RoutePath + "\\PATHS\\" + player.path;
			Train train = new Train(Program.Simulator);
			train.TrainType = Train.TRAINTYPE.REMOTE;
			if (MPManager.IsServer()) //server needs to worry about correct train number
			{
			}
			else
			{
				train.Number = player.num;
			}
			int direction = player.dir;
			train.travelled = player.Travelled;
			try
			{
				train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, player.TileX, player.TileZ, player.X, player.Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			}
			catch (Exception e)
			{
				if (MPManager.IsServer())
				{
					MPManager.BroadCast((new MSGMessage(player.user, "Error", "MultiPlayer Error：" + e.Message)).ToString());
				}
				throw new MultiPlayerError();
			}
			TrainCar previousCar = null;
			for (var i = 0; i < player.cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				string wagonFilePath = Program.Simulator.BasePath + @"\trains\trainset\" + player.cars[i];
				try
				{
					TrainCar car = RollingStock.Load(Program.Simulator, wagonFilePath, previousCar);
					bool flip = true;
					if (player.flipped[i] == 0) flip = false;
					car.Flipped = flip;
					car.CarID = player.ids[i];
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
				catch (Exception error)
				{
					System.Console.WriteLine(error);
				}
			}// for each rail car

			if (train.Cars.Count == 0)
			{
				throw (new Exception("The train of player " + player.user + " is empty from"));
			}

			p.Username = player.user;
			train.CalculatePositionOfCars(0);
			train.InitializeBrakes();
			train.InitializeSignals(false);
			train.CheckFreight();
			train.LeadLocomotive = null;
			train.LeadNextLocomotive();
			foreach (var car in train.Cars) {
				if (car.CarID == p.LeadingLocomotiveID) train.LeadLocomotive = car;
			}
			p.Train = train;
			MPManager.Instance().AddOrRemoveTrain(train, true);

		}
	}
}
