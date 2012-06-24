using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;
using MSTS;
namespace ORTS.MultiPlayer
{
	public class Message
	{
		public string msg;
		public static Message Decode(string m)
		{
			int index = m.IndexOf(' ');
			string key = m.Substring(0, index);
			if (key == "MOVE") return new MSGMove(m.Substring(index + 1));
			else if (key == "PLAYER") return new MSGPlayer(m.Substring(index + 1));
			else if (key == "SWITCH") return new MSGSwitch(m.Substring(index + 1));
			else if (key == "SWITCHSTATES") return new MSGSwitchStatus(m.Substring(index + 1));
			else if (key == "TRAIN") return new MSGTrain(m.Substring(index + 1));
			else if (key == "REMOVETRAIN") return new MSGRemoveTrain(m.Substring(index + 1));
			else if (key == "SERVER") return new MSGServer(m.Substring(index + 1));
			else if (key == "MESSAGE") return new MSGMessage(m.Substring(index + 1));
			else if (key == "EVENT") return new MSGEvent(m.Substring(index + 1));
			else if (key == "UNCOUPLE") return new MSGUncouple(m.Substring(index + 1));
			else if (key == "COUPLE") return new MSGCouple(m.Substring(index + 1));
			else if (key == "QUIT") return new MSGQuit(m.Substring(index + 1));
			else throw new Exception("Unknown Keyword" + key);
		}

		public virtual void HandleMsg() { System.Console.WriteLine("test"); return; }
	}

	public class MSGMove : Message
	{
		class MSGMoveItem
		{
			public string user;
			public float speed;
			public float travelled;
			public int num;
			public int TileX, TileZ, trackNodeIndex;
			public float X, Z;
			public MSGMoveItem(string u, float s, float t, int n, int tX, int tZ, float x, float z, int tni)
			{
				user = u; speed = s; travelled = t; num = n; TileX = tX; TileZ = tZ; X = x; Z = z; trackNodeIndex = tni;
			}
			public override string ToString()
			{
				return user + " " + speed + " " + travelled + " " + num + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + trackNodeIndex;
			}
		}
		List<MSGMoveItem> items;
		public MSGMove(string m)
		{
			m = m.Trim();
			string[] areas = m.Split(' ');
			if (areas.Length%9  != 0) //user speed travelled
			{
				throw new Exception("Parsing error " + m);
			}
			try
			{
				int i = 0;
				items = new List<MSGMoveItem>();
				for (i = 0; i < areas.Length / 9; i++)
					items.Add(new MSGMoveItem(areas[9 * i], float.Parse(areas[9 * i + 1]), float.Parse(areas[9 * i + 2]), int.Parse(areas[9 * i + 3]), int.Parse(areas[9 * i + 4]), int.Parse(areas[9 * i + 5]), float.Parse(areas[9 * i + 6]), float.Parse(areas[9 * i + 7]), int.Parse(areas[9 * i + 8])));
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public MSGMove()
		{
		}

		public void AddNewItem(string u, Train t)
		{
			if (items == null) items = new List<MSGMoveItem>();
			items.Add(new MSGMoveItem(u, t.SpeedMpS, t.travelled, t.Number, t.RearTDBTraveller.TileX, t.RearTDBTraveller.TileZ, t.RearTDBTraveller.X, t.RearTDBTraveller.Z, t.RearTDBTraveller.TrackNodeIndex));
			t.LastReportedSpeed = t.SpeedMpS;
		}

		public override string ToString()
		{
			string tmp = "MOVE ";
			for (var i = 0; i < items.Count; i++) tmp += items[i].ToString() + " ";
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			foreach (MSGMoveItem m in items)
			{
				if (m.user == MPManager.GetUserName()) continue; //about itself, ignore
				if (m.user.Contains("0xAI") || m.user.Contains("0xUC"))
				{
					foreach (Train t in Program.Simulator.Trains)
					{
						if (t.Number == m.num)
						{
							if (t is RemoteTrain)
							{
								RemoteTrain t1 = (RemoteTrain)t;
								if (t1 != null)
								{
									t1.ToDoUpdate(m.trackNodeIndex, m.TileX, m.TileZ, m.X, m.Z, m.travelled, m.speed);
									break;
								}
							}
						}
					}
				}
				else
				{
					RemoteTrain t = (RemoteTrain)MPManager.OnlineTrains.findTrain(m.user);
					/*
					if (t != null)
					{
						bool moved = t.RearTDBTraveller.MoveTo(tileX, tileZ, X, Y, Z);				// = new TDBTraveller(tileX, tileZ, X, Z, this.Direction, Program.Simulator.TDB, Program.Simulator.TSectionDat);
						if (moved == false)
						{
							System.Console.Write("N");
							t.RearTDBTraveller = new TDBTraveller(tileX, tileZ, X, Z, this.Direction, Program.Simulator.TDB, Program.Simulator.TSectionDat);
							//
						}
						//t.RepositionRearTraveller();
						//t.CalculatePositionOfCars(0);//
						t.SpeedMpS = this.Speed;
					}*/
					if (t != null)
					{
						t.ToDoUpdate(m.trackNodeIndex, m.TileX, m.TileZ, m.X, m.Z, m.travelled, m.speed);
						//t.CalculatePositionOfCars(m.travelled - t.travelled);
					}
				}
			}
		}
	}

	public class MSGPlayer : Message
	{
		public string user = "";
		public string code = "";
		public int num; //train number
		public string con; //consist
		public string path; //path consist and path will always be double quoted
		public string route;
		public int dir; //direction
		public int TileX, TileZ;
		public float X, Z, Travelled;
		public double seconds;
		public int season, weather;
		public int pantofirst, pantosecond;

		public string[] cars;
		public string[] ids;
		public int[] flipped; //if a wagon is engine

		public MSGPlayer(string m)
		{
			string[] areas = m.Split('\r');
			if (areas.Length <= 5)
			{
				throw new Exception("Parsing error in MSGPlayer" + m);
			}
			try
			{
				var tmp = areas[0].Trim();
				string[] data = tmp.Split(' ');
				user = data[0];
				code = data[1];
				num = int.Parse(data[2]);
				TileX = int.Parse(data[3]);
				TileZ = int.Parse(data[4]);
				X = float.Parse(data[5]);
				Z = float.Parse(data[6]);
				Travelled = float.Parse(data[7]);
				seconds = double.Parse(data[8]);
				season = int.Parse(data[9]);
				weather = int.Parse(data[10]);
				pantofirst = int.Parse(data[11]);
				pantosecond = int.Parse(data[12]);
				//user = areas[0].Trim();
				con = areas[1].Trim();
				route = areas[2].Trim();
				path = areas[3].Trim();
				dir = int.Parse(areas[4].Trim());
				ParseTrainCars(areas[5].Trim());
				int index = path.LastIndexOf("\\PATHS\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					path = path.Remove(0, index + 7);
				}
				index = con.LastIndexOf("\\CONSISTS\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					con = con.Remove(0, index + 10);
				}

			}
			catch (Exception e)
			{
				throw e;
			}
		}


		private void ParseTrainCars(string m)
		{
			string[] areas = m.Split('\t');
			var numCars = areas.Length;
			if (MPManager.IsServer()) if (numCars <= 0) throw new MultiPlayerError();
				else if (numCars <= 0) throw new Exception();
			cars = new string[numCars];//with an empty "" at end
			ids = new string[numCars];
			flipped = new int[numCars];
			int index, last;
			for (var i = 0; i < numCars; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
			}

		}
		public MSGPlayer(string n, string cd, string c, string p, Train t, int tn)
		{
			route = Program.Simulator.RouteName;
			int index = p.LastIndexOf("\\PATHS\\", StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				p = p.Remove(0, index + 7);
			}
			index = c.LastIndexOf("\\CONSISTS\\", StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				c = c.Remove(0, index + 10);
			}
			user = n; code = cd; con = c; path = p;
			if (t != null)
			{
				dir = (int)t.RearTDBTraveller.Direction; num = tn; TileX = t.RearTDBTraveller.TileX;
				TileZ = t.RearTDBTraveller.TileZ; X = t.RearTDBTraveller.X; Z = t.RearTDBTraveller.Z; Travelled = t.travelled;
			}
			seconds = Program.Simulator.ClockTime; season = (int)Program.Simulator.Season; weather = (int)Program.Simulator.Weather;
			pantofirst = pantosecond = 0;
			MSTSWagon w = (MSTSWagon)Program.Simulator.PlayerLocomotive;
			if (w != null)
			{
				pantofirst = w.AftPanUp == true ? 1 : 0;
				pantosecond = w.FrontPanUp == true ? 1 : 0;
			}

			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].WagFilePath;
				ids[i] = t.Cars[i].CarID;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}

		}
		public override string ToString()
		{
			string tmp = "PLAYER " + user + " " + code + " " + num + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " " + seconds + " " + season + " " + weather + " " + pantofirst + " " + pantosecond + " \r" + con + "\r" + route + "\r" + path + "\r" + dir + "\r";
			for (var i = 0; i < cars.Length; i++)
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\t";
			}

			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			MPManager.OnlineTrains.AddPlayers(this, null);
			//System.Console.WriteLine(this.ToString());
			if (MPManager.IsServer())// && Program.Server.IsRemoteServer())
			{
				MSGSwitchStatus msg2 = new MSGSwitchStatus();
				MPManager.BroadCast(msg2.ToString());

				MSGPlayer host = new MSGPlayer(MPManager.GetUserName(), "1234", Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.PlayerLocomotive.Train,
					Program.Simulator.PlayerLocomotive.Train.Number);

				MPManager.BroadCast(host.ToString() + MPManager.OnlineTrains.AddAllPlayerTrain());

				foreach (Train t in Program.Simulator.Trains)
				{
					if (t == Program.Simulator.PlayerLocomotive.Train) continue; //avoid broadcast player train
					if (MPManager.OnlineTrains.findTrain(t)) continue;
					MPManager.BroadCast((new MSGTrain(t, t.Number)).ToString());
				}

				//System.Console.WriteLine(host.ToString() + Program.Simulator.OnlineTrains.AddAllPlayerTrain());

			}
			else //client needs to handle environment
			{
				if (MPManager.GetUserName() == this.user) //a reply from the server, update my train number
				{
					Program.Simulator.PlayerLocomotive.Train.Number = this.num;
				}
				Program.Simulator.Weather = (WeatherType)this.weather;
				Program.Simulator.ClockTime = this.seconds;
				Program.Simulator.Season = (SeasonType)this.season;
			}

		}

		public void HandleMsg(OnlinePlayer p)
		{
			if (!MPManager.IsServer()) return; //only intended for the server, when it gets the player message in OnlinePlayer Receive

			MPManager.OnlineTrains.AddPlayers(this, p);
			//System.Console.WriteLine(this.ToString());
			MSGSwitchStatus msg2 = new MSGSwitchStatus();
			MPManager.BroadCast(msg2.ToString());

			MSGPlayer host = new MSGPlayer(MPManager.GetUserName(), "1234", Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.PlayerLocomotive.Train,
				Program.Simulator.PlayerLocomotive.Train.Number);

			MPManager.BroadCast(host.ToString() + MPManager.OnlineTrains.AddAllPlayerTrain());

			foreach (Train t in Program.Simulator.Trains)
			{
				if (t == Program.Simulator.PlayerLocomotive.Train) continue; //avoid broadcast player train
				if (MPManager.OnlineTrains.findTrain(t)) continue;
				MPManager.BroadCast((new MSGTrain(t, t.Number)).ToString());
			}

			//System.Console.WriteLine(host.ToString() + Program.Simulator.OnlineTrains.AddAllPlayerTrain());

		}
	}


	public class MSGSwitch : Message
	{
		public string user;
		public int TileX, TileZ, WorldID, Selection;

		public MSGSwitch(string m)
		{
			string[] tmp = m.Split(' ');
			if (tmp.Length != 5) throw new Exception("Parsing error " + m);
			user = tmp[0];
			TileX = int.Parse(tmp[1]);
			TileZ = int.Parse(tmp[2]);
			WorldID = int.Parse(tmp[3]);
			Selection = int.Parse(tmp[4]);
		}

		public MSGSwitch(string n, int tX, int tZ, int u, int s)
		{
			user = n;
			WorldID = u;
			TileX = tX;
			TileZ = tZ;
			Selection = s;
		}

		public override string ToString()
		{
			string tmp = "SWITCH " + user + " " + TileX + " " + TileZ + " " + WorldID + " " + Selection;
			//System.Console.WriteLine(tmp);
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			//System.Console.WriteLine(this.ToString());

			if (user == MPManager.GetUserName()) return;//ignore myself
			TrJunctionNode trj = Program.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, WorldID);
			trj.SelectedRoute = Selection;
			MPManager.BroadCast(this.ToString()); //server will tell others
		}
	}

	public class MSGSignal : Message
	{
		public SignalObject signal;

	}

	public class MSGSwitchStatus : Message
	{
		static SortedList<long, TrJunctionNode> SwitchState;
		string msgx = "";

		public MSGSwitchStatus()
		{
			if (SwitchState == null)
			{
				SwitchState = new SortedList<long, TrJunctionNode>();
				int key = 0;
				foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
				{
					if (t != null && t.TrJunctionNode != null)
					{
						key = t.UiD.WorldTileX * 100000000 + t.UiD.WorldTileZ * 10000 + t.UiD.WorldID;
						SwitchState.Add(key, t.TrJunctionNode);
					}
				}
			}
			msgx = "";
			foreach(System.Collections.Generic.KeyValuePair<long, TrJunctionNode> t in SwitchState)
			{
				if (t.Value.SelectedRoute > 9 && t.Value.SelectedRoute < 0)
				{
					throw new Exception("Selected route is " + t.Value.SelectedRoute + ". Please inform OR for the problem");
				}
				msgx += t.Value.SelectedRoute;
			}
		}

		public MSGSwitchStatus(string m)
		{
			if (SwitchState == null)
			{
				int key = 0;
				SwitchState = new SortedList<long, TrJunctionNode>();
				foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
				{
					if (t != null && t.TrJunctionNode != null)
					{
						key = t.UiD.WorldTileX * 100000000 + t.UiD.WorldTileZ * 10000 + t.UiD.WorldID;
						SwitchState.Add(key, t.TrJunctionNode);
					}
				}

			}
			msgx = m;
		}

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer() ) return; //server will ignore it

			int i = 0;
			foreach (System.Collections.Generic.KeyValuePair<long, TrJunctionNode> t in SwitchState)
			{
				t.Value.SelectedRoute = msgx[i] - 48; //ASCII code 48 is 0
				i++;
			}
			//System.Console.WriteLine(msg);

		}

		public override string ToString()
		{
			string tmp = "SWITCHSTATES " + msgx;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGTrain : Message
	{
		string[] cars;
		string[] ids;
		int[] flipped; //if a wagon is engine
		int TrainNum;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		public MSGTrain(string m)
		{
			//System.Console.WriteLine(m);
			int index = m.IndexOf(' '); int last = 0;
			TrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			direction = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileX = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileZ = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			X = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Z = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Travelled = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			string[] areas = m.Split('\t');
			cars = new string[areas.Length-1];//with an empty "" at end
			ids = new string[areas.Length - 1];
			flipped = new int[areas.Length - 1];
			for (var i = 0; i < cars.Length; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
			}

			//System.Console.WriteLine(this.ToString());

		}
		public MSGTrain(Train t, int n)
		{
			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].WagFilePath;
				ids[i] = t.Cars[i].CarID;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}
			TrainNum = n;
			direction = t.RearTDBTraveller.Direction==Traveller.TravellerDirection.Forward?1:0;
			TileX = t.RearTDBTraveller.TileX;
			TileZ = t.RearTDBTraveller.TileZ;
			X = t.RearTDBTraveller.X;
			Z = t.RearTDBTraveller.Z;
			Travelled = t.travelled;
		}

		public override void HandleMsg() //only client will get message, thus will set states
		{
			if (MPManager.IsServer()) return; //server will ignore it
			//System.Console.WriteLine(this.ToString());
			// construct train data
			foreach (Train t in Program.Simulator.Trains)
			{
				if (t.Number == this.TrainNum) return; //already add it
			}
			Train train = new RemoteTrain(Program.Simulator);
			int consistDirection = direction;
			train.travelled = Travelled;
			train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			//if (consistDirection != 1)
			//	train.RearTDBTraveller.ReverseDirection();
			TrainCar previousCar = null;
			for(var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				string wagonFilePath = Program.Simulator.BasePath + @"\trains\trainset\" + cars[i];
				try
				{
					TrainCar car = RollingStock.Load(Program.Simulator, wagonFilePath, previousCar);
					bool flip = true;
					if (flipped[i] == 0) flip = false;
					car.Flipped = flip ;
					car.CarID = ids[i];
					train.Cars.Add(car);
					car.Train = train;
					previousCar = car;
				}
				catch (Exception error)
				{
					System.Console.WriteLine( wagonFilePath +" " + error);
				}
			}// for each rail car

			if (train.Cars.Count == 0) return;

			train.CalculatePositionOfCars(0);
			train.InitializeBrakes();
			train.InitializeSignals(false);

			train.Number = this.TrainNum;
			if (train.Cars[0] is MSTSLocomotive) train.LeadLocomotive = train.Cars[0];
			MPManager.Instance().AddAddedTrains(train); //the train to be added and treated later, to be thread safe


		}

		public override string ToString()
		{
			string tmp = "TRAIN " + TrainNum + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " ";
			for(var i = 0; i < cars.Length; i++) 
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\t";
			}
			return "" + tmp.Length + ": " + tmp;
		}
	}

	//remove AI trains
	public class MSGRemoveTrain : Message
	{
		public List<int> trains;

		public MSGRemoveTrain(string m)
		{
			string[] tmp = m.Split(' ');
			trains = new List<int>();
			for (var i = 0; i < tmp.Length; i++)
			{
				trains.Add(int.Parse(tmp[i]));
			}
		}

		public MSGRemoveTrain(List<Train> ts)
		{
			trains = new List<int>();
			foreach (Train t in ts)
			{
				trains.Add(t.Number);
			}
		}

		public override string ToString()
		{

			string tmp = "REMOVETRAIN";
			foreach (int i in trains)
			{
				tmp += " " + i;
			}
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			foreach (int i in trains)
			{
				foreach (Train train in Program.Simulator.Trains)
				{
					if (i == train.Number)
					{
						MPManager.Instance().AddRemovedTrains(train); //added to the removed list, treated later to be thread safe
					}
				}
			}
		}

	}

	public class MSGServer : Message
	{
		public MSGServer(string m)
		{
		}


		public override string ToString()
		{
			string tmp = "SERVER YOU";
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (Program.Server != null) return; //already a server, not need to worry
			Program.Server = new Server(Program.Client.UserName + ' ' + Program.Client.Code, Program.Client);
			//System.Console.WriteLine(this.ToString());
		}
	}

	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGTrainMerge : Message
	{
		int TrainNumRetain;
		int TrainNumRemoved;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		public MSGTrainMerge(string m)
		{
			m = m.Trim();
			string[] areas = m.Split(' ');
			TrainNumRetain = int.Parse(areas[0]);
			TrainNumRemoved = int.Parse(areas[1]);
			direction = int.Parse(areas[2]);
			TileX = int.Parse(areas[3]);
			TileZ = int.Parse(areas[4]);
			X = float.Parse(areas[5]);
			Z = float.Parse(areas[6]);
			Travelled = float.Parse(areas[7]);
		}
		public MSGTrainMerge(Train t1, Train t2)
		{
			TrainNumRetain = t1.Number;
			TrainNumRemoved = t2.Number;
			direction = t1.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 1 : 0; 
			TileX = t1.RearTDBTraveller.TileX;
			TileZ = t1.RearTDBTraveller.TileZ;
			X = t1.RearTDBTraveller.X;
			Z = t1.RearTDBTraveller.Z;
			Travelled = t1.travelled;

		}

		public override void HandleMsg() 
		{

		}

		public override string ToString()
		{
			string tmp = "TRAINMERGE " + TrainNumRetain + " " + TrainNumRemoved + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	//message to add new train from either a string (received message), or a Train (building a message)
	public class MSGMessage : Message
	{
		string msgx;
		string level;
		string user; 
		public MSGMessage(string m)
		{
			m.Trim();
			string[] t = m.Split('\t');
			user = t[0];
			level = t[1];
			msgx = t[2];
		}

		public MSGMessage(string u, string l, string m)
		{
			user = u;
			level = l;

			msgx = m;
		}

		public override void HandleMsg()
		{
			if (MPManager.GetUserName() == user)
			{
				Program.Simulator.Confirmer.Message(level, msgx + " will be in single mode");
				if (level == "Error" && !MPManager.IsServer())//if is a client, fatal error, will close the connection, and get into single mode
				{
					MPManager.Notify((new MSGQuit(MPManager.GetUserName())).ToString());//to be nice, still send a quit before close the connection
					throw new MultiPlayerError();//this is a fatal error, thus the client will be stopped in ClientComm
				}
			}
		}

		public override string ToString()
		{
			string tmp = "MESSAGE " + user + "\t" + level + "\t" + msgx;
			return "" + tmp.Length + ": " + tmp;
		}
	}

	public class MSGEvent : Message
	{
		public string user;
		public string EventName;
		public int EventState;

		public MSGEvent(string m)
		{
			string[] tmp = m.Split(' '); 
			if (tmp.Length != 3) throw new Exception("Parsing error " + m);
			user = tmp[0].Trim();
			EventName = tmp[1].Trim();
			EventState = int.Parse(tmp[2]);
		}

		public MSGEvent(string m, string e, int ID)
		{
			user = m.Trim();
			EventName = e;
			EventState = ID;
		}

		public override string ToString()
		{

			string tmp = "EVENT " + user + " " + EventName + " " + EventState;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) return; //avoid myself
			Train t = MPManager.OnlineTrains.findTrain(user);
			if (t == null) return;

			if (EventName == "HORN")
			{
				t.SignalEvent(EventState);
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "PANTO2")
			{
				MSTSWagon w = (MSTSWagon)t.Cars[0];
				if (w == null) return;

				w.FrontPanUp = (EventState == 1 ? true : false);

				foreach (TrainCar car in t.Cars)
					if (car is MSTSWagon) ((MSTSWagon)car).FrontPanUp = w.FrontPanUp;
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "PANTO1")
			{
				MSTSWagon w = (MSTSWagon)t.Cars[0];
				if (w == null) return;

				w.AftPanUp = (EventState == 1 ? true : false);

				foreach (TrainCar car in t.Cars)
					if (car is MSTSWagon) ((MSTSWagon)car).AftPanUp = w.AftPanUp;
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else if (EventName == "BELL")
			{
				if (t.LeadLocomotive != null) t.LeadLocomotive.SignalEvent(EventState == 0 ? EventID.BellOff : EventID.BellOn);
			}
			else if (EventName == "WIPER")
			{
				if (t.LeadLocomotive != null) t.LeadLocomotive.SignalEvent(EventState == 0 ? EventID.WiperOff : EventID.WiperOn);
			}
			else if (EventName == "HEADLIGHT")
			{
				if (t.LeadLocomotive != null && EventState == 0) t.LeadLocomotive.SignalEvent(EventID.HeadlightOff);
				if (t.LeadLocomotive != null && EventState == 1) t.LeadLocomotive.SignalEvent(EventID.HeadlightDim);
				if (t.LeadLocomotive != null && EventState == 2) t.LeadLocomotive.SignalEvent(EventID.HeadlightOn);
			}
			else return;
		}

	}

	public class MSGQuit : Message
	{
		public string user;

		public MSGQuit(string m)
		{
			user = m.Trim();
		}

		public override string ToString()
		{

			string tmp = "QUIT " + user;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) return; //avoid myself

			bool ServerQuit = false;
			if (Program.Client != null && user.Contains("ServerHasToQuit")) //the server quits, will send a message with ServerHasToQuit\tServerName
			{
				Program.Simulator.Confirmer.Message("Error", "Server quits, will play as single mode");
				user = user.Replace("ServerHasToQuit\t", ""); //get the user name of server from the message
				ServerQuit = true;
			}
			if (MPManager.IsServer())
			{
				OnlinePlayer p = MPManager.OnlineTrains.Players[user];
				if (p != null)
				{
					lock (Program.Server.Players)
					{
						Program.Server.Players.Remove(p);
					}
					MPManager.Instance().AddRemovedPlayer(p);
					if (p.thread != null) p.thread.Abort();//end communication with this player
				}
				MPManager.BroadCast(this.ToString()); //if the server, will broadcast
			}
			else //client will remove train
			{
				OnlinePlayer p = MPManager.OnlineTrains.Players[user];
				if (p != null)
				{
					MPManager.Instance().AddRemovedPlayer(p);
					if (ServerQuit) throw new MultiPlayerError(); //server quit, end communication by throwing this error 
				}
			}
		}

	}

	public class MSGUncouple : Message
	{
		public string user, newTrainName, carID, firstCarIDOld, firstCarIDNew;
		public int TileX1, TileZ1;
		public float X1, Z1, Travelled1, Speed1;
		public int trainDirection;
		public int TileX2, TileZ2;
		public float X2, Z2, Travelled2, Speed2;
		public int train2Direction;
		public int newTrainNumber;
		public int oldTrainNumber;
		public int whichIsPlayer;
		public MSGUncouple(string m)
		{
			string[] areas = m.Split('\t');
			try
			{
				carID = areas[0];
				string[] tmp = areas[1].Split(' ');
				user = tmp[0];
				TileX1 = int.Parse(tmp[1]); TileZ1 = int.Parse(tmp[2]);
				X1 = float.Parse(tmp[3]); Z1 = float.Parse(tmp[4]); Travelled1 = float.Parse(tmp[5]); Speed1 = float.Parse(tmp[6]); trainDirection = int.Parse(tmp[7]);
				oldTrainNumber = int.Parse(tmp[8]);
				tmp = areas[2].Split(' ');
				newTrainName = tmp[0]; 
				TileX2 = int.Parse(tmp[1]); TileZ2 = int.Parse(tmp[2]);
				X2 = float.Parse(tmp[3]); Z2 = float.Parse(tmp[4]); Travelled2 = float.Parse(tmp[5]); Speed2 = float.Parse(tmp[6]); train2Direction = int.Parse(tmp[7]);
				newTrainNumber = int.Parse(tmp[8]);
				firstCarIDOld = areas[3];
				firstCarIDNew = areas[4];

				whichIsPlayer = int.Parse(areas[5]);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public MSGUncouple(Train t, Train newT, string u, string ID, TrainCar car)
		{
			carID = ID;
			user = u;
			TileX1 = t.RearTDBTraveller.TileX; TileZ1 = t.RearTDBTraveller.TileZ; X1 = t.RearTDBTraveller.X; Z1 = t.RearTDBTraveller.Z; Travelled1 = t.travelled; Speed1 = t.SpeedMpS;
			trainDirection = t.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 0 : 1;//0 forward, 1 backward
			TileX2 = newT.RearTDBTraveller.TileX; TileZ2 = newT.RearTDBTraveller.TileZ; X2 = newT.RearTDBTraveller.X; Z2 = newT.RearTDBTraveller.Z; Travelled2 = newT.travelled; Speed2 = newT.SpeedMpS;
			train2Direction = newT.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 0 : 1;//0 forward, 1 backward
			if (MPManager.IsServer()) newTrainNumber = newT.Number;//serer will use the correct number
			else newTrainNumber = 1000000 + Program.Random.Next(1000000);//client: temporary assign a train number 1000000-2000000, will change to the correct one after receiving response from the server
			
			oldTrainNumber = t.Number;
			newTrainName = "UC" + newTrainNumber; newT.Number = newTrainNumber;
			
			firstCarIDNew = newT.Cars[0].CarID;
			firstCarIDOld = t.Cars[0].CarID;

			//to see which train contains the car (PlayerLocomotive)
			if (t.Cars.Contains(car)) whichIsPlayer = 0;
			else if (newT.Cars.Contains(car)) whichIsPlayer = 1;
			else whichIsPlayer = 2;
		}

		public override string ToString()
		{
			string tmp = "UNCOUPLE " + carID + "\t" + user + " " +
				TileX1 + " " + TileZ1 + " " + X1 + " " + Z1 + " " + Travelled1 + " " + Speed1 + " " + trainDirection + " " + oldTrainNumber + "\t" +
				newTrainName + " " +
				TileX2 + " " + TileZ2 + " " + X2 + " " + Z2 + " " + Travelled2 + " " + Speed2 + " " + train2Direction + " " + newTrainNumber + "\t" + 
				firstCarIDOld + "\t" + firstCarIDNew + "\t" + whichIsPlayer;
			return "" + tmp.Length + ": " + tmp;
		}

		public override void HandleMsg()
		{
			if (user == MPManager.GetUserName()) //received from the server, but it is about mine action of uncouple
			{
				foreach (Train t in Program.Simulator.Trains)
				{
					foreach (TrainCar car in t.Cars)
					{
						if (car.CarID == firstCarIDOld)//got response about this train
						{
							t.Number = oldTrainNumber;
						}
						if (car.CarID == firstCarIDNew)//got response about this train
						{
							t.Number = newTrainNumber;
						}
					}
				}

			}
			else
			{
				Train train = MPManager.OnlineTrains.findTrain(user);
				if (train == null) return;
				int i = 0;
				while (i < train.Cars.Count && train.Cars[i].CarID != carID) ++i;  // it can't happen that car isn't in car.Train
				if (i == train.Cars.Count - 1) return;  // can't uncouple behind last car
				++i;
				TrainCar lead = train.LeadLocomotive;
				// move rest of cars to the new train
				Train train2 = new RemoteTrain(Program.Simulator);
				for (int k = i; k < train.Cars.Count; ++k)
				{
					TrainCar newcar = train.Cars[k];
					train2.Cars.Add(newcar);
					newcar.Train = train2;
				}

				// and drop them from the old train
				for (int k = train.Cars.Count - 1; k >= i; --k)
				{
					train.Cars.RemoveAt(k);
				}

				Traveller.TravellerDirection d1 = Traveller.TravellerDirection.Forward;
				if (trainDirection == 1) d1 = Traveller.TravellerDirection.Backward;

				Traveller.TravellerDirection d2 = Traveller.TravellerDirection.Forward;
				if (train2Direction == 1) d2 = Traveller.TravellerDirection.Backward;
				
				train.LastCar.CouplerSlackM = 0;

				// and fix up the travellers
				train2.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX2, TileZ2, X2, Z2, d2);
				train2.travelled = Travelled2;
				train2.SpeedMpS = Speed2;

				train2.CalculatePositionOfCars(0);  // fix the front traveller

				train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX1, TileZ1, X1, Z1, d1);
				train.CalculatePositionOfCars(0);  // fix the front traveller
				train.travelled = Travelled1;
				train.SpeedMpS = Speed1;


				train2.InitializeSignals(false);
				train2.LeadLocomotive = lead;
				MPManager.Instance().AddAddedTrains(train2);
				train.LeadLocomotive = lead;
				train.UncoupledFrom = train2;
				train2.UncoupledFrom = train;
				train2.SpeedMpS = train.SpeedMpS;

				train.Update(0);   // stop the wheels from moving etc
				train2.Update(0);  // stop the wheels from moving etc

				if (whichIsPlayer == 0 && MPManager.OnlineTrains.findTrain(user) != null) MPManager.OnlineTrains.Players[user].Train = train;
				else if (whichIsPlayer == 1 && MPManager.OnlineTrains.findTrain(user) != null) MPManager.OnlineTrains.Players[user].Train = train2; //the player may need to update the train it drives

				if (MPManager.IsServer())
				{
					this.newTrainNumber = train2.Number;//we got a new train number, will tell others.
					this.oldTrainNumber = train.Number;
					MPManager.BroadCast(this.ToString());//if server receives this, will tell others, including whoever sent the information
				}
				else
				{
					train2.Number = this.newTrainNumber; //client receives a message, will use the train number specified by the server
					train.Number = this.oldTrainNumber;
				}
			}
		}
	}

	public class MSGCouple : Message
	{
		string[] cars;
		string[] ids;
		int[] flipped; //if a wagon is engine
		int TrainNum;
		int RemovedTrainNum;
		int direction;
		int TileX, TileZ;
		float X, Z, Travelled;
		public MSGCouple(string m)
		{
			//System.Console.WriteLine(m);
			int index = m.IndexOf(' '); int last = 0;
			TrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			RemovedTrainNum = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			direction = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileX = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			TileZ = int.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			X = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Z = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			index = m.IndexOf(' ');
			Travelled = float.Parse(m.Substring(0, index + 1));
			m = m.Remove(0, index + 1);
			string[] areas = m.Split('\t');
			cars = new string[areas.Length - 1];//with an empty "" at end
			ids = new string[areas.Length - 1];
			flipped = new int[areas.Length - 1];
			for (var i = 0; i < cars.Length; i++)
			{
				index = areas[i].IndexOf('\"');
				last = areas[i].LastIndexOf('\"');
				cars[i] = areas[i].Substring(index + 1, last - index - 1);
				string tmp = areas[i].Remove(0, last + 1);
				tmp = tmp.Trim();
				string[] carinfo = tmp.Split('\n');
				ids[i] = carinfo[0];
				flipped[i] = int.Parse(carinfo[1]);
			}

			//System.Console.WriteLine(this.ToString());

		}

		public MSGCouple(Train t, Train oldT)
		{
			cars = new string[t.Cars.Count];
			ids = new string[t.Cars.Count];
			flipped = new int[t.Cars.Count];
			for (var i = 0; i < t.Cars.Count; i++)
			{
				cars[i] = t.Cars[i].WagFilePath;
				ids[i] = t.Cars[i].CarID;
				if (t.Cars[i].Flipped == true) flipped[i] = 1;
				else flipped[i] = 0;
			}
			TrainNum = t.Number;
			RemovedTrainNum = oldT.Number;
			direction = t.RearTDBTraveller.Direction == Traveller.TravellerDirection.Forward ? 1 : 0;
			TileX = t.RearTDBTraveller.TileX;
			TileZ = t.RearTDBTraveller.TileZ;
			X = t.RearTDBTraveller.X;
			Z = t.RearTDBTraveller.Z;
			Travelled = t.travelled;
			MPManager.Instance().RemoveUncoupledTrains(t); //remove the trains from uncoupled train lists
			MPManager.Instance().RemoveUncoupledTrains(oldT);

		}

		public override string ToString()
		{
			string tmp = "COUPLE " + TrainNum + " " + RemovedTrainNum + " " + direction + " " + TileX + " " + TileZ + " " + X + " " + Z + " " + Travelled + " ";
			for (var i = 0; i < cars.Length; i++)
			{
				var c = cars[i];
				var index = c.LastIndexOf("\\trains\\trainset\\", StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					c = c.Remove(0, index + 17);
				}//c: wagon path without folder name

				tmp += "\"" + c + "\"" + " " + ids[i] + "\n" + flipped[i] + "\t";
			}
			return "" + tmp.Length + ": " + tmp;
		}

		private TrainCar FindCar(Train t1, Train t2, string carID)
		{
			foreach (TrainCar c in t1.Cars) if (c.CarID == carID) return c;
			foreach (TrainCar c in t2.Cars) if (c.CarID == carID) return c;
			return null;
		}
		public override void HandleMsg()
		{
			if (MPManager.IsServer()) return;//server will not receive this from client
			string PlayerTrainID;
			if (Program.Simulator.PlayerLocomotive != null) PlayerTrainID = Program.Simulator.PlayerLocomotive.CarID;
			else PlayerTrainID = "NULL";
			Train train = null, train2 = null;

			foreach (Train t in Program.Simulator.Trains)
			{
				if (t.Number == this.TrainNum) train = t;
				if (t.Number == this.RemovedTrainNum) train2 = t;
			}

			if (Program.Simulator.PlayerLocomotive != null && Program.Simulator.PlayerLocomotive.Train == train2)
			{
				Train tmp = train2; train2 = train;	train = tmp;
			}

			if (train == null || train2 == null) return; //did not find the trains to op on

			int consistDirection = direction;
			train.travelled = Travelled;
			train.RearTDBTraveller = new Traveller(Program.Simulator.TSectionDat, Program.Simulator.TDB.TrackDB.TrackNodes, TileX, TileZ, X, Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
			//if (consistDirection != 1)
			//	train.RearTDBTraveller.ReverseDirection();
			TrainCar previousCar = null;
			List<TrainCar> tmpCars = new List<TrainCar>();
			for (var i = 0; i < cars.Length; i++)// cars.Length-1; i >= 0; i--) {
			{
				TrainCar car = FindCar(train, train2, ids[i]);
				if (car == null) continue;
				bool flip = true;
				if (flipped[i] == 0) flip = false;
				car.Flipped = flip;
				car.CarID = ids[i];
				tmpCars.Add(car);
				car.Train = train;
				previousCar = car;

			}// for each rail car
			//List<TrainCar> oldList = train.Cars;
			train.Cars = tmpCars;
			//oldList.Clear();

			if (train.Cars.Count == 0) return;

			train.CalculatePositionOfCars(0);
			train.LeadLocomotive = train.Cars[0];
			if (train.LeadLocomotive == null) train.LeadNextLocomotive();

			if (Program.Simulator.PlayerLocomotive != null)
			{
				train.LeadLocomotive = Program.Simulator.PlayerLocomotive;
			}

			MPManager.Instance().AddRemovedTrains(train2);
		}
	}

	public class MSGSignalStatus : Message
	{
		//static SortedList<long, SignalItem> SignalState;
		//string msg = "";

		//public MSGSignalStatus()
		//{
		//    if (SignalState == null)
		//    {
		//        SignalState = new SortedList<long, TrJunctionNode>();
		//        int key = 0;
		//        foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
		//        {
		//            if (t != null && t.TrJunctionNode != null)
		//            {
		//                key = t.UiD.WorldTileX * 100000000 + t.UiD.WorldTileZ * 10000 + t.UiD.WorldID;
		//                SignalState.Add(key, t.TrJunctionNode);
		//            }
		//        }
		//    }
		//    msg = "";
		//    foreach (System.Collections.Generic.KeyValuePair<long, TrJunctionNode> t in SignalState)
		//    {
		//        if (t.Value.SelectedRoute > 9 && t.Value.SelectedRoute < 0)
		//        {
		//            throw new Exception("Selected route is " + t.Value.SelectedRoute + ". Please inform OR for the problem");
		//        }
		//        msg += t.Value.SelectedRoute;
		//    }
		//}

		//public MSGSignalStatus(string m)
		//{
		//    if (SignalState == null)
		//    {
		//        int key = 0;
		//        SignalState = new SortedList<long, TrJunctionNode>();
		//        foreach (TrackNode t in Program.Simulator.TDB.TrackDB.TrackNodes)
		//        {
		//            if (t != null && t.TrJunctionNode != null)
		//            {
		//                key = t.UiD.WorldTileX * 100000000 + t.UiD.WorldTileZ * 10000 + t.UiD.WorldID;
		//                SignalState.Add(key, t.TrJunctionNode);
		//            }
		//        }

		//    }
		//    msg = m;
		//}

		//public override void HandleMsg() //only client will get message, thus will set states
		//{
		//    if (Program.Server != null) return; //server will ignore it

		//    int i = 0;
		//    foreach (System.Collections.Generic.KeyValuePair<long, TrJunctionNode> t in SignalState)
		//    {
		//        t.Value.SelectedRoute = msg[i] - 48; //ASCII code 48 is 0
		//        i++;
		//    }
		//    //System.Console.WriteLine(msg);

		//}

		//public override string ToString()
		//{
		//    string tmp = "SWITCHSTATES " + msg;
		//    return "" + tmp.Length + ": " + tmp;
		//}
	}

}
