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

/* MPManager
 * 
 * Contains code to manager multiplayer sessions, especially to hide server/client mode from other code.
 * For example, the Notify method will check if it is server (then broadcast) or client (then send msg to server)
 * but the caller does not need to care.
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
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
		public int version = 15;
		double lastMoveTime = 0.0f;
		public double lastSwitchTime = 0.0f;
		double lastSendTime = 0.0f;
		string metric = "";
		double metricbase = 1.0f;
		public static OnlineTrains OnlineTrains = new OnlineTrains();
		private static MPManager localUser = null;

		public List<Train> removedTrains;
		private List<Train> addedTrains;

		private List<Train> uncoupledTrains;

		public bool weatherChanged = false;
		public bool weatherChangHandled = false;
		public int newWeather = -1;
        public float newFog = -1f;
        public float overCast = -1f;

		public double lastPlayerAddedTime = 0.0f;
		public int MPUpdateInterval = 10;
		static public bool AllowedManualSwitch = true;
		public bool TrySwitch = true;
		public bool AllowNewPlayer = true;
		public bool ComposingText = false;
		public string lastSender = ""; //who last sends me a message
		public bool AmAider = false; //am I aiding the dispatcher?
		public List<string> aiderList;
		public Dictionary<string, OnlinePlayer> lostPlayer = new Dictionary<string,OnlinePlayer>();
		public bool NotServer = true;
		public static DispatchViewer DispatcherWindow;
		public bool CheckSpad = true;
		public static bool PreferGreen = true;
		Simulator Simulator;
		Viewer3D Viewer;
		public string MD5Check = "";

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
			var str = msg.ToString();
			var index = str.IndexOf("SWITCHSTATES ");
			OriginalSwitchState = str.Remove(0, index + 13);
		}
		//handles singleton
		private MPManager()
		{
			playersRemoved = new List<OnlinePlayer>();
			uncoupledTrains = new List<Train>();
			addedTrains = new List<Train>();
			removedTrains = new List<Train>();
			aiderList = new List<string>();
			if (Program.Server != null) NotServer = false;
			users = new SortedList<double,string>();
			GetMD5HashFromTDBFile();
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
			if (NotServer == true && Program.Server != null) //I was a server, but no longer
			{
				Program.Server = null;
				if (Program.DebugViewer != null) Program.DebugViewer.firstShow = true;
			}
			else if (NotServer == false && Program.Server == null) //I am declared the server
			{
				Program.Server = new Server(Program.Client.UserName + ' ' + Program.Client.Code, Program.Client);
				if (Program.DebugViewer != null) Program.DebugViewer.firstShow = true;
			}
			//get key strokes and determine if some messages should be sent
			handleUserInput();

			if (begineZeroTime == 0) begineZeroTime = newtime - 10;

			CheckPlayerTrainSpad();//over speed or pass a red light

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

			//some players are disconnected more than 1 minute ago, will not care if they come back later
			CleanLostPlayers();

			//some trains are added/removed
			HandleTrainList();

			AddPlayer(); //a new player joined? handle it

			/* will have this in the future so that helpers can also control
			//I am a helper, will see if I need to update throttle and dynamic brake
			if (Program.Simulator.PlayerLocomotive.Train != null && Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) 
			{

			}
			 * */
		}


		void CheckPlayerTrainSpad()
		{
			if (CheckSpad == false) return;
			var Locomotive = (MSTSLocomotive)Program.Simulator.PlayerLocomotive;
			if (Locomotive == null) return;
			var train = Locomotive.Train;
			if (train == null ||train.TrainType == Train.TRAINTYPE.REMOTE) return;//no train or is remotely controlled

			var spad = false;
			var maxSpeed = Math.Abs(train.AllowedMaxSpeedMpS) + 3;//allow some margin of error (about 10km/h)
			var speed = Math.Abs(Locomotive.SpeedMpS);
			if (speed > maxSpeed) spad = true;
			//if (train.TMaspect == ORTS.Popups.TrackMonitorSignalAspect.Stop && Math.Abs(train.distanceToSignal) < 2*speed && speed > 5) spad = true; //red light and cannot stop within 2 seconds, if the speed is large

#if !NEW_SIGNALLING
			if (spad == true || train.spad2)
			{
				Locomotive.SetEmergency();
				Program.Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
				train.spad2 = false;
			}
#endif


		}
		//check if it is in the server mode
		public static bool IsServer()
		{
			if (Program.Server != null) return true;
			else return false;
		}

		//check if it is in the client mode
		public static bool IsClient()
		{
			if (!MPManager.IsMultiPlayer() || MPManager.IsServer()) return false;
			return true;
		}
        //check if it is in the server mode && they are players && not allow autoswitch
        public static bool NoAutoSwitch()
        {
            if (!MPManager.IsMultiPlayer()) return false;
            //if (MPManager.IsClient()) return true;
            return !MPManager.AllowedManualSwitch; //aloow manual switch or not
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
			if (m == null) return;
			if (Program.Server != null) Program.Server.BroadCast(m);
		}

		//notify others (server will broadcast, client will send msg to server)
		static public void Notify(string m)
		{
			if (m == null) return;
			if (Program.Client != null && Program.Server == null) Program.Client.Send(m); //client notify server
			if (Program.Server != null) Program.Server.BroadCast(m); //server notify everybody else
		}

		static public void SendToServer(string m)
		{
			if (m!= null && Program.Client != null) Program.Client.Send(m);
		}
		static public void BroadcastSignal()
		{
		}

#if !NEW_SIGNALLING
		static public void BroadcastSignal(Signal s)
		{

		}
#endif

		public static void StopDispatcher()
		{
			if (DispatcherWindow != null) { if (MPManager.Instance().Viewer != null) MPManager.Instance().Viewer.DebugViewerEnabled = false; Stopped = true; DispatcherWindow.Visible = false; }
		}
		public static bool Stopped = false;
		//nicely shutdown listening threads, and notify the server/other player
		static public void Stop()
		{
			Stopped = true;
			StopDispatcher();
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

		public bool PlayerAdded = false;

		public void AddPlayer()
		{
			if (!MPManager.IsServer()) return;
			if (PlayerAdded == true)
			{
				PlayerAdded = false;
				MPManager.Instance().lastPlayerAddedTime = Program.Simulator.GameTime;
				MPManager.Instance().lastSwitchTime = Program.Simulator.GameTime;

				MSGPlayer host = new MSGPlayer(MPManager.GetUserName(), "1234", Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.PlayerLocomotive.Train,
					Program.Simulator.PlayerLocomotive.Train.Number, Program.Simulator.Settings.AvatarURL);
				MPManager.BroadCast(host.ToString() + MPManager.OnlineTrains.AddAllPlayerTrain());
				foreach (Train t in Program.Simulator.Trains)
				{
					if (Program.Simulator.PlayerLocomotive != null && t == Program.Simulator.PlayerLocomotive.Train) continue; //avoid broadcast player train
					if (MPManager.Instance().FindPlayerTrain(t)) continue;
					if (removedTrains.Contains(t)) continue;//this train is going to be removed, should avoid it.
					MPManager.BroadCast((new MSGTrain(t, t.Number)).ToString());
				}
				if (CheckSpad == false) { MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGMessage("All", "OverSpeedOK", "OK to go overspeed and pass stop light")).ToString()); }
				else { MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGMessage("All", "NoOverSpeed", "Penalty for overspeed and passing stop light")).ToString()); }
                MPManager.BroadCast(GetEnvInfo());
			}
		}

        //create weather message
        public string GetEnvInfo()
        {
            return (new MSGWeather(-1, overCast, newFog)).ToString();//update weather

        }

        //set weather message
        public void SetEnvInfo(float o, float f)
        {
            newFog = f;
            overCast = o;

        }

        //this will be used in the server, in Simulator.cs
		public bool TrainOK2Couple(Train t1, Train t2)
		{
			//if (Math.Abs(t1.SpeedMpS) > 10 || Math.Abs(t2.SpeedMpS) > 10) return false; //we do not like high speed punch in MP, will mess up a lot.

			if (t1.TrainType != Train.TRAINTYPE.REMOTE && t2.TrainType != Train.TRAINTYPE.REMOTE) return true;

			bool result = true;
			try
			{
				foreach (var p in OnlineTrains.Players)
				{
					if (p.Value.Train == t1 && Program.Simulator.GameTime  - p.Value.CreatedTime < 120) { result = false; break; }
					if (p.Value.Train == t2 && Program.Simulator.GameTime - p.Value.CreatedTime < 120) { result = false; break; }
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
		
		SortedList<double, string> users;

		public string GetOnlineUsersInfo()
		{

			string info = "";
			if (Program.Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) info = "Your locomotive is a helper\t";
			info += ("" + (OnlineTrains.Players.Count + 1)+ (OnlineTrains.Players.Count <= 0 ? " player " : "  players "));
			info += ("" + Program.Simulator.Trains.Count + (Program.Simulator.Trains.Count <= 1 ? " train" : "  trains"));
			TrainCar mine = Program.Simulator.PlayerLocomotive;
			users.Clear();
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

			int count = 0;
			foreach (var pair in users)
			{
				if (count >= 10) break;
				info += "\t" + pair.Value + ": distance of " + (int)(pair.Key / metricbase) + metric;
				count++;
			}
			if (count < OnlineTrains.Players.Count) { info += "\t ..."; }
			return info;
		}

		private List<OnlinePlayer> playersRemoved;
		public void AddRemovedPlayer(OnlinePlayer p)
		{
			lock (playersRemoved)
			{
				if (playersRemoved.Contains(p)) return;
				playersRemoved.Add(p);
			}
		}

		private void CleanLostPlayers()
		{
			//check if any of the lost player list has been lost for more than 60 seconds. If so, remove it and will not worry about it anymore
			if (lostPlayer.Count > 0)
			{
				List<string> removeLost = null;
				foreach (var x in lostPlayer)
				{
					if (Program.Simulator.GameTime - x.Value.quitTime > 60)
					{
						if (removeLost == null) removeLost = new List<string>();
						removeLost.Add(x.Key);
					}
				}
				if (removeLost != null)
				{
					foreach (var name in removeLost)
					{
						lostPlayer.Remove(name);
					}
				}
			}
		}
		//only can be called by Update
		private void RemovePlayer()
		{
			//if (Program.Server == null) return; //client will do it by decoding message
			if (playersRemoved.Count == 0) return;

			try //do it with lock, but may still have exception
			{
				lock (playersRemoved)
				{
					foreach (OnlinePlayer p in playersRemoved)
					{
						if (Program.Server != null) Program.Server.Players.Remove(p);
						//player is not in this train
						if (p.Train != null && p.Train != Program.Simulator.PlayerLocomotive.Train)
						{
#if !NEW_SIGNALLING
							if (p.Train.TrackAuthority != null)
							{
								Program.Simulator.AI.Dispatcher.SetAuthorization(p.Train.TrackAuthority, null, null, 0);
								Program.Simulator.AI.Dispatcher.Unreserve(p.Train.Number + 100000);
								Program.Simulator.AI.Dispatcher.TrackAuthorities.Remove(p.Train.TrackAuthority);
								p.Train.TrackAuthority = null;
							}
#endif

							//make sure this train has no other player on it
							bool hasOtherPlayer = false;
							foreach (var p1 in OnlineTrains.Players)
							{
								if (p == p1.Value) continue;
								if (p1.Value.Train == p.Train) { hasOtherPlayer = true; break; }//other player has the same train
							}
                            if (hasOtherPlayer == false) { p.Train.RemoveFromTrack(); Program.Simulator.Trains.Remove(p.Train); }
						}
						OnlineTrains.Players.Remove(p.Username);
					}
				}
			}
			catch (Exception e)
			{
				System.Console.WriteLine(e + e.StackTrace); return;
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
#if !NEW_SIGNALLING
						if (IsServer())
						{
							if (t.Path != null && !PreferGreen)
							{
								t.TrackAuthority = new TrackAuthority(t, t.Number + 100000, 10, t.Path);
								Program.Simulator.AI.Dispatcher.TrackAuthorities.Add(t.TrackAuthority);
								Program.Simulator.AI.Dispatcher.RequestAuth(t, true, 0);
								//t.Path.AlignInitSwitches(t.RearTDBTraveller, -1, 500);
							}
							else t.TrackAuthority = null;
						}
#endif
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
                        t.RemoveFromTrack();
						Program.Simulator.Trains.Remove(t);
#if !NEW_SIGNALLING
						if (t.TrackAuthority != null)
						{
							Program.Simulator.AI.Dispatcher.SetAuthorization(t.TrackAuthority, null, null, 0);
							Program.Simulator.AI.Dispatcher.Unreserve(t.Number + 100000);
							Program.Simulator.AI.Dispatcher.TrackAuthorities.Remove(t.TrackAuthority);
							t.TrackAuthority = null;
						}
#endif
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

            if (UserInput.IsPressed(UserCommands.ControlHorn)) Notify((new MSGEvent(GetUserName(), "HORN", 1)).ToString());

            if (UserInput.IsReleased(UserCommands.ControlHorn)) Notify((new MSGEvent(GetUserName(), "HORN", 0)).ToString());
			
			if (UserInput.IsPressed(UserCommands.ControlPantograph2)) Notify((new MSGEvent(GetUserName(), "PANTO2", (++PantoSecondCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlPantograph1)) Notify((new MSGEvent(GetUserName(), "PANTO1", (++PantoFirstCount)%2)).ToString());

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

		public void HandleDispatcherWindow(Simulator simulator, Viewer3D viewer)
		{
			Simulator = simulator;
			Viewer = viewer;
			Thread t = new Thread(StartDispatcher);
			t.Start();
		}

		void StartDispatcher()
		{
			DispatcherWindow = new DispatchViewer(Simulator, Viewer);
			Program.DebugViewer = DispatcherWindow;
			DispatcherWindow.Show();
			DispatcherWindow.Hide();
			while (MPManager.Stopped != true)
			{
				if (Viewer.DebugViewerEnabled == true)
				{
					if (DispatcherWindow.Visible != true) DispatcherWindow.ShowDialog();
					DispatcherWindow.Visible = true;
				}
				else
				{
					DispatcherWindow.Hide();
				}
				Thread.Sleep(100);
			}
			DispatcherWindow.Dispose();
		}
        public TrainCar SubCar(string wagonFilePath, int length)
		{
			System.Console.WriteLine("Will substitute with your existing stocks\n.");
			TrainCar car = null;
			try
			{
				char type = 'w';
				if (wagonFilePath.ToLower().Contains(".eng")) type = 'e';
				string newWagonFilePath = SubMissingCar(length, type);
                car = RollingStock.Load(Program.Simulator, newWagonFilePath);
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

		//md5 check of TDB file
		public void GetMD5HashFromTDBFile()
		{
			try
			{
				string fileName = Program.Simulator.RoutePath + @"\" + Program.Simulator.TRK.Tr_RouteFile.FileName + ".tdb";
				FileStream file = new FileStream(fileName, FileMode.Open);
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] retVal = md5.ComputeHash(file);
				file.Close();

				MD5Check = Encoding.Unicode.GetString(retVal, 0, retVal.Length);
			}
			catch
			{
				System.Console.WriteLine("Cannot get MD5 check of TDB file, server may not connect you");
				MD5Check = "";
			}
		}
	}
}
