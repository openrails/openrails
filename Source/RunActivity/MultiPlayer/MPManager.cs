/* MPManager
 * 
 * Contains code to manager multiplayer sessions, especially to hide server/client mode from other code.
 * For example, the Notify method will check if it is server (then broadcast) or client (then send msg to server)
 * but the caller does not need to care.
 * 
 * 
 */

// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

/// 
/// Additional Contributions
/// Copyright (c) Jijun Tang
/// Can only be used by the Open Rails Project.
/// This file cannot be copied, modified or included in any software which is not distributed directly by the Open Rails project.
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ORTS;
using ORTS.Debugging;
using System.Threading;
using MSTS;

namespace ORTS.MultiPlayer
{
	//a singleton class handles communication, update and stop etc.
	class MPManager
	{
		double lastMoveTime = 0.0f;
		public double lastSwitchTime = 0.0f;
		double lastSendTime = 0.0f;
		string metric = "";
		double metricbase = 1.0f;
		public static OnlineTrains OnlineTrains = new OnlineTrains();
		private static MPManager localUser = null;

		private List<Train> removedTrains;
		private List<Train> addedTrains;

		private List<Train> uncoupledTrains;

		public bool weatherChanged = false;
		public bool weatherChangHandled = false;
		public int newWeather;
		public float overCast;

		public double lastPlayerAddedTime = 0.0f;
		public int MPUpdateInterval = 10;
		public bool ClientAllowedSwitch = true;
		public bool ComposingText = false;
		public string lastSender = ""; //who last sends me a message
		public void AddUncoupledTrains(Train t)
		{
			lock (uncoupledTrains)
			{
				uncoupledTrains.Add(t);
			}
		}

		public void RemoveUncoupledTrains(Train t)
		{
			lock (uncoupledTrains)
			{
				uncoupledTrains.Remove(t);
			}
		}

		public void MoveUncoupledTrains(MSGMove move)
		{
			if (uncoupledTrains != null && uncoupledTrains.Count > 0)
			{
				foreach (Train t in uncoupledTrains)
				{
					if (t != null)
					{
						if (Math.Abs(t.SpeedMpS) > 0.001) move.AddNewItem("0xUC" + t.Number, t);
						else if (Math.Abs(t.LastReportedSpeed) > 0) move.AddNewItem("0xUC" + t.Number, t);
					}
				}
			}
		}

		public string OriginalSwitchState = "";

		public void RememberOriginalSwitchState()
		{
			MSGSwitchStatus msg = new MSGSwitchStatus();
			OriginalSwitchState = msg.msgx;
		}
		//handles singleton
		private MPManager()
		{
			playersRemoved = new List<OnlinePlayer>();
			uncoupledTrains = new List<Train>();
			addedTrains = new List<Train>();
			removedTrains = new List<Train>();
		}
		public static MPManager Instance()
		{
			if (localUser == null) localUser = new MPManager();
			return localUser;
		}

		public void RequestControl()
		{
			try
			{
				Train train = Program.Simulator.PlayerLocomotive.Train;
				
				MSGControl msgctl;
				//I am the server, I have control
				if (IsServer())
				{
					train.TrainType = Train.TRAINTYPE.PLAYER; train.LeadLocomotive = Program.Simulator.PlayerLocomotive;
					if (Program.Simulator.Confirmer != null)
						Program.Simulator.Confirmer.Information("You gained back the control of your train");
					msgctl = new MSGControl(GetUserName(), "Confirm", train);
					BroadCast(msgctl.ToString());
				}
				else //client, send request
				{
					msgctl = new MSGControl(GetUserName(), "Request", train);
					SendToServer(msgctl.ToString());
				}
			}
			catch (Exception)
			{ }
		}


		public void RequestSignalReset()
		{
			try
			{
				if (IsServer())
				{
					return;
				}
				else //client, send request
				{
					MSGResetSignal msgctl = new MSGResetSignal(GetUserName());
					SendToServer(msgctl.ToString());
				}
			}
			catch (Exception)
			{ }
		}

		double previousSpeed = 0;
		double begineZeroTime = 0;
		/// <summary>
		/// Update. Determines what messages to send every some seconds
		/// 1. every one second will send train location
		/// 2. by defaulr, every 10 seconds will send switch/signal status, this can be changed by in the menu of setting MPUpdateInterval
		/// 3. housekeeping (remove/add trains, remove players)
		/// 4. it will also capture key stroke of horn, panto, wiper, bell, headlight etc.
		/// </summary>

		public void Update(double newtime)
		{
			//get key strokes and determine if some messages should be sent
			handleUserInput();

			if (begineZeroTime == 0) begineZeroTime = newtime - 10;
			//server update train location of all
			if (Program.Server != null && newtime - lastMoveTime >= 1f)
			{
				MSGMove move = new MSGMove();
				move.AddNewItem(GetUserName(), Program.Simulator.PlayerLocomotive.Train);
				Program.Server.BroadCast(OnlineTrains.MoveTrains(move));
				lastMoveTime = lastSendTime = newtime;

#if INDIVIDUAL_CONTROL
				if (Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE)
				{
					Program.Server.BroadCast((new MSGLocoInfo(Program.Simulator.PlayerLocomotive, GetUserName())).ToString());
				}
#endif
			}
			
			//server updates switch
			if (Program.Server != null && newtime - lastSwitchTime >= MPUpdateInterval)
			{
				lastSwitchTime = lastSendTime = newtime;
				var switchStatus = new MSGSwitchStatus();

				if (switchStatus.OKtoSend) BroadCast(switchStatus.ToString());
				var signalStatus = new MSGSignalStatus();
				if (signalStatus.OKtoSend) BroadCast(signalStatus.ToString());

			}
			
			//client updates itself
			if (Program.Client != null && Program.Server == null && newtime - lastMoveTime >= 1f)
			{
				Train t = Program.Simulator.PlayerLocomotive.Train;
				MSGMove move = new MSGMove();
				//if I am still conrolling the train
				if (t.TrainType != Train.TRAINTYPE.REMOTE)
				{
					if (Math.Abs(t.SpeedMpS) > 0.001 || newtime - begineZeroTime < 5f) move.AddNewItem(GetUserName(), t);
					else if (Math.Abs(t.LastReportedSpeed) > 0) move.AddNewItem(GetUserName(), t);
					else
					{
						if (Math.Abs(previousSpeed) > 0.001)
						{
							begineZeroTime = newtime;
						}
					}

				}
				//MoveUncoupledTrains(move); //if there are uncoupled trains
				//if there are messages to send
				if (move.OKtoSend())
				{
					Program.Client.Send(move.ToString());
					lastMoveTime = lastSendTime = newtime;
				}
				previousSpeed = t.SpeedMpS;

#if INDIVIDUAL_CONTROL

				if (Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE)
				{
					Program.Client.Send((new MSGLocoInfo(Program.Simulator.PlayerLocomotive, GetUserName())).ToString());
				}
#endif
			}


			//need to send a keep-alive message if have not sent one to the server for the last 30 seconds
			if (Program.Client != null && Program.Server == null && newtime - lastSendTime >= 30f)
			{
				Notify((new MSGAlive(GetUserName())).ToString());
				lastSendTime = newtime;
			}

			//some players are removed
			RemovePlayer();

			//some trains are added/removed
			HandleTrainList();

			/* will have this in the future so that helpers can also control
			//I am a helper, will see if I need to update throttle and dynamic brake
			if (Program.Simulator.PlayerLocomotive.Train != null && Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) 
			{

			}
			 * */
		}

		//check if it is in the server mode
		public static bool IsServer()
		{
			if (Program.Server != null) return true;
			else return false;
		}

		//check if it is in the server mode
		public static bool IsClient()
		{
			if (Program.Server == null && Program.Client == null) return false;
			if (Program.Server == null) return true;
			else return false;
		}
		//user name
		static public string GetUserName()
		{
			if (Program.Server != null) return Program.Server.UserName;
			else if (Program.Client != null) return Program.Client.UserName;
			else return "";
		}

		//check if it is in the multiplayer session
		static public bool IsMultiPlayer()
		{
			if (Program.Server != null || Program.Client != null) return true;
			else return false;
		}

		static public void BroadCast(string m)
		{
			if (Program.Server != null) Program.Server.BroadCast(m);
		}

		//notify others (server will broadcast, client will send msg to server)
		static public void Notify(string m)
		{
			if (Program.Client != null && Program.Server == null) Program.Client.Send(m); //client notify server
			if (Program.Server != null) Program.Server.BroadCast(m); //server notify everybody else
		}

		static public void SendToServer(string m)
		{
			if (Program.Client != null) Program.Client.Send(m);
		}
		static public void BroadcastSignal()
		{
		}
		static public void BroadcastSignal(Signal s)
		{

		}

		//nicely shutdown listening threads, and notify the server/other player
		static public void Stop()
		{
			if (Program.Client != null && Program.Server == null)
			{
				Program.Client.Send((new MSGQuit(GetUserName())).ToString()); //client notify server
				Thread.Sleep(1000);
				Program.Client.Stop();
			}
			if (Program.Server != null)
			{
				Program.Server.BroadCast((new MSGQuit("ServerHasToQuit\t"+GetUserName())).ToString()); //server notify everybody else
				Thread.Sleep(1000);
				if (Program.Server.ServerComm != null) Program.Server.Stop();
				if (Program.Client != null) Program.Client.Stop();
			}
			
		}

		//when two player trains connected, require decouple at speed 0.
		public bool TrainOK2Decouple(Train t)
		{
			if (t == null) return false;
			if (Math.Abs(t.SpeedMpS) < 0.001) return true;
			try
			{
				var count = 0;
				foreach (var p in OnlineTrains.Players.Keys)
				{
					string p1 = p + " ";
					foreach (var car in t.Cars)
					{
						if (car.CarID.Contains(p1)) count++;
					}
				}
				if (count >= 2)
				{
					if (Program.Simulator.Confirmer != null)
						Program.Simulator.Confirmer.Information("Cannot decouple: train has " + count + " players, need to completely stop.");
					return false;
				}
			}
			catch { return false; }
			return true;
		}

		//this will be used in the server, in Simulator.cs
		public bool TrainOK2Couple(Train t1, Train t2)
		{

			if (t1.TrainType != Train.TRAINTYPE.REMOTE && t2.TrainType != Train.TRAINTYPE.REMOTE) return true;

			bool result = true;
			try
			{
				foreach (var p in OnlineTrains.Players)
				{
					if (p.Value.Train == t1 && Program.Simulator.GameTime  - p.Value.CreatedTime < 20) { result = false; break; }
					if (p.Value.Train == t2 && Program.Simulator.GameTime - p.Value.CreatedTime < 20) { result = false; break; }
				}
			}
			catch (Exception)
			{
			}
			return result;
		}
		/// <summary>
		/// Return a string of information of how many players online and those users who are close
		/// </summary>
		public string GetOnlineUsersInfo()
		{

			string info = "";
			if (Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) info = "Your locomotive is a helper\t";
			info += ("" + (OnlineTrains.Players.Count + 1)+ (OnlineTrains.Players.Count <= 0 ? " player " : "  players "));
			info += ("" + Program.Simulator.Trains.Count + (Program.Simulator.Trains.Count <= 1 ? " train" : "  trains"));
			TrainCar mine = Program.Simulator.PlayerLocomotive;
			SortedList<double, string> users = new SortedList<double,string>();
			try//the list of players may be changed during the following process
			{
				//foreach (var train in Program.Simulator.Trains) info += "\t" + train.Number + " " + train.Cars.Count;
				//info += "\t" + MPManager.OnlineTrains.Players.Count;
				//foreach (var p in MPManager.OnlineTrains.Players) info += "\t" + p.Value.Train.Number + " " + p.Key;
				foreach (OnlinePlayer p in OnlineTrains.Players.Values)
				{
					if (p.Train == null) continue;
					if (p.Train.Cars.Count <= 0) continue;
					var d = WorldLocation.GetDistanceSquared(p.Train.RearTDBTraveller.WorldLocation, mine.Train.RearTDBTraveller.WorldLocation);
					users.Add(Math.Sqrt(d)+Program.Random.NextDouble(), p.Username);
				}
			}
			catch (Exception)
			{
			}
			if (metric == "")
			{
				metric = Program.Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric == true ? " m" : " yd";
				metricbase = Program.Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric == true ? 1.0f : 1.0936133f;
			}

			foreach(var pair in users)
			{
				info += "\t" + pair.Value + ": distance of " + (int)(pair.Key/metricbase) + metric;
			}
			return info;
		}

		private List<OnlinePlayer> playersRemoved;
		public void AddRemovedPlayer(OnlinePlayer p)
		{
			lock (playersRemoved)
			{
				playersRemoved.Add(p);
			}
		}

		//only can be called by Update
		private void RemovePlayer()
		{
			//if (Program.Server == null) return; //client will do it by decoding message
			if (playersRemoved.Count == 0) return;

			try //do it without lock, so may have exception
			{
				foreach (OnlinePlayer p in playersRemoved)
				{
					if (Program.Server != null) Program.Server.Players.Remove(p);
					OnlineTrains.Players.Remove(p.Username);
					//player is not in this train
					if (p.Train != Program.Simulator.PlayerLocomotive.Train)
					{
						Program.Simulator.Trains.Remove(p.Train);
						if (p.Train.TrackAuthority != null)
						{
							Program.Simulator.AI.Dispatcher.SetAuthorization(p.Train.TrackAuthority, null, null, 0);
							Program.Simulator.AI.Dispatcher.Unreserve(p.Train.Number + 100000);
							Program.Simulator.AI.Dispatcher.TrackAuthorities.Remove(p.Train.TrackAuthority);
							p.Train.TrackAuthority = null;
						}

					}
				}
			}
			catch (Exception e)
			{
				return;
			}
			playersRemoved.Clear();
		}

		public bool AddOrRemoveTrain(Train t, bool add)
		{
			if (add)
			{
				lock (addedTrains)
				{
					foreach (var t1 in addedTrains)
					{
						if (t1.Number == t.Number) return false;
					}
					addedTrains.Add(t); return true;
				}
			}
			else
			{
				lock (removedTrains)
				{
					removedTrains.Add(t); return true;
				}
			}
		}

		//only can be called by Update
		private void HandleTrainList()
		{
			if (addedTrains.Count != 0)
			{

				try //do it without lock, so may have exception
				{
					foreach (var t in addedTrains)
					{
						var hasIt = false;
						foreach (var t1 in Program.Simulator.Trains)
						{
							if (t1.Number == t.Number) { hasIt = true; break; }
						}
						if (!hasIt) Program.Simulator.Trains.Add(t);
						if (IsServer())
						{
							if (t.Path != null)
							{
								t.TrackAuthority = new TrackAuthority(t, t.Number + 100000, 10, t.Path);
								Program.Simulator.AI.Dispatcher.TrackAuthorities.Add(t.TrackAuthority);
								Program.Simulator.AI.Dispatcher.RequestAuth(t, true, 0);
								t.Path.AlignInitSwitches(t.RearTDBTraveller, -1, 500);
							}
							else t.TrackAuthority = null;
						}

					}
					addedTrains.Clear();
				}
				catch (Exception) { }
			}
			if (removedTrains.Count != 0)
			{

				try //do it without lock, so may have exception
				{
					foreach (var t in removedTrains)
					{
						Program.Simulator.Trains.Remove(t);
						if (t.TrackAuthority != null)
						{
							Program.Simulator.AI.Dispatcher.SetAuthorization(t.TrackAuthority, null, null, 0);
							Program.Simulator.AI.Dispatcher.Unreserve(t.Number + 100000);
							Program.Simulator.AI.Dispatcher.TrackAuthorities.Remove(t.TrackAuthority);
							t.TrackAuthority = null;
						}

					}
					removedTrains.Clear();
				}
				catch (Exception) { }
			}
		}

		public Train FindPlayerTrain(string user)
		{
			return OnlineTrains.findTrain(user);
		}

		public bool FindPlayerTrain(Train t)
		{
			return OnlineTrains.findTrain(t);
		}

		public static void LocoChange(Train t, TrainCar lead)
		{
			Notify((new MSGLocoChange(GetUserName(), lead.CarID, t)).ToString());
		}
		//count how many times a key has been stroked, thus know if the panto should be up or down, etc. for example, stroke 11 times means up, thus send event with id 1
		int PantoSecondCount = 0;
		int PantoFirstCount = 0;
		int BellCount = 0;
		int WiperCount = 0;
		int HeadLightCount = 0;

		public void handleUserInput()
		{
			TrainCar Locomotive = Program.Simulator.PlayerLocomotive;
			//In Multiplayer, I maybe the helper, but I can request to be the controller
			if (UserInput.IsPressed(UserCommands.GameRequestControl))
			{
				RequestControl();
			}

			if (UserInput.IsPressed(UserCommands.ControlHorn))	Notify((new MSGEvent(GetUserName(), "HORN", EventID.HornOn)).ToString());

			if (UserInput.IsReleased(UserCommands.ControlHorn)) Notify((new MSGEvent(GetUserName(), "HORN", EventID.HornOff)).ToString());
			
			if (UserInput.IsPressed(UserCommands.ControlPantographSecond)) Notify((new MSGEvent(GetUserName(), "PANTO2", (++PantoSecondCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlPantographFirst)) Notify((new MSGEvent(GetUserName(), "PANTO1", (++PantoFirstCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlBell)) Notify((new MSGEvent(GetUserName(), "BELL", (++BellCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlWiper)) Notify((new MSGEvent(GetUserName(), "WIPER", (++WiperCount) % 2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlHeadlightIncrease))
			{
				HeadLightCount++; if (HeadLightCount >= 3) HeadLightCount = 2;
				Notify((new MSGEvent(GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}

			if (UserInput.IsPressed(UserCommands.ControlHeadlightDecrease))
			{
				HeadLightCount--; if (HeadLightCount < 0) HeadLightCount = 0;
				Notify((new MSGEvent(GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}

		}

		public TrainCar SubCar(string wagonFilePath, int length, TrainCar previousCar)
		{
			System.Console.WriteLine("Will substitute with your existing stocks\n.");
			TrainCar car = null;
			try
			{
				char type = 'w';
				if (wagonFilePath.ToLower().Contains(".eng")) type = 'e';
				string newWagonFilePath = SubMissingCar(length, type);
				car = RollingStock.Load(Program.Simulator, newWagonFilePath, previousCar);
				car.Length = length;
				car.RealWagFilePath = wagonFilePath;
				if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Information("Missing car, have substituted with other one.");

			}
			catch (Exception error)
			{
				System.Console.WriteLine(error.Message + "Substitution failed, will ignore it\n.");
				car = null;
			}
			return car;
		}
		SortedList<double, string> coachList = null;
		SortedList<double, string> engList = null;

		public string SubMissingCar(int length, char type)
		{

			type = char.ToLower(type);
			SortedList<double, string> copyList;
			if (type == 'w')
			{
				if (coachList == null)
					coachList = GetList(type);
				copyList = coachList;
			}
			else
			{
				if (engList == null)
					engList = GetList(type);
				copyList = engList;
			}
			string bestName = "Default\\default.wag"; double bestDist = 1000;

			foreach (var item in copyList)
			{
				var dist = Math.Abs(item.Key - length);
				if (dist < bestDist) { bestDist = dist; bestName = item.Value; }
			}
			return Program.Simulator.BasePath + "\\trains\\trainset\\" + bestName;

		}

		SortedList<double, string> GetList(char type)
		{
			string ending = "*.eng";
			if (type == 'w') ending = "*.wag";
			string[] filePaths = Directory.GetFiles(Program.Simulator.BasePath + "\\trains\\trainset", ending, SearchOption.AllDirectories);
			string temp;
			List<string> allEngines = new List<string>();
			SortedList<double, string> carList = new SortedList<double, string>();
			for (var i = 0; i < filePaths.Length; i++)
			{
				int index = filePaths[i].LastIndexOf("\\trains\\trainset\\");
				temp = filePaths[i].Substring(index + 17);
				if (!temp.Contains("\\")) continue;
				allEngines.Add(temp);
			}
			foreach (string name in allEngines)
			{
				double len = 0.0f;
				try
				{
					using (STFReader stf = new STFReader(Program.Simulator.BasePath + "\\trains\\trainset\\" + name, true))
						while (!stf.Eof)
						{
							string token = stf.ReadItem();
							if (stf.Tree.ToLower() == "wagon(size")
							{
								stf.MustMatch("(");
								stf.ReadFloat(STFReader.UNITS.Distance, null);
								stf.ReadFloat(STFReader.UNITS.Distance, null);
								len = stf.ReadFloat(STFReader.UNITS.Distance, null);
								break;
							}
						}
					carList.Add(len + Program.Random.NextDouble() / 10.0f, name);
				}
				catch { }
			}
			return carList;
		}
	}
}
