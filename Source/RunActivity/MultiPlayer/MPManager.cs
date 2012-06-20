/* MPManager
 * 
 * Contains code to manager multiplayer sessions, especially to hide server/client mode from other code.
 * For example, the Notify method will check if it is server (then broadcast) or client (then send msg to server)
 * but the caller does not need to care.
 * 
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */
#define DEBUG_VIEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;
using ORTS.Debugging;
using System.Threading;

namespace ORTS.MultiPlayer
{
	//a singleton class handles communication, update and stop etc.
	class MPManager
	{
		double lastMoveTime = 0.0f;
		double lastSwitchTime = 0.0f;
		string metric = "";
		double metricbase = 1.0f;
		public static OnlineTrains OnlineTrains = new OnlineTrains();
		private static MPManager localUser = null;

		//handles singleton
		private MPManager()
		{
		}
		public static MPManager Instance()
		{
			if (localUser == null) localUser = new MPManager();
			return localUser;
		}

		/// <summary>
		/// Update. Determines what messages to send every some seconds
		/// 1. every one second will send train location
		/// 2. every five seconds will send switch status
		/// 3. it will also capture key stroke of horn, panto, wiper, bell, headlight etc.
		/// </summary>

		public void Update(double newtime)
		{
			//get key strokes and determine if some messages should be sent
			handleUserInput();

			//server update train location of all
			if (Program.Server != null && newtime - lastMoveTime >= 1f)
			{

				Traveller t = Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller;
				MultiPlayer.MSGMove move = new MultiPlayer.MSGMove();
				move.AddNewItem(MultiPlayer.MPManager.GetUserName(), Program.Simulator.PlayerLocomotive.Train);
				Program.Server.BroadCast(OnlineTrains.MoveTrains(move));
				lastMoveTime = newtime;
			}
			
			//server updates switch
			if (Program.Server != null && newtime - lastSwitchTime >= 10f)
			{
				lastSwitchTime = newtime;
				MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGSwitchStatus()).ToString());
			}
			
			//client updates itself
			if (Program.Client != null && Program.Server == null && newtime - lastMoveTime >= 1f)
			{
				Traveller t = Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller;
				MultiPlayer.MSGMove move = new MultiPlayer.MSGMove();
				move.AddNewItem(MultiPlayer.MPManager.GetUserName(), Program.Simulator.PlayerLocomotive.Train);
				Program.Client.Send(move.ToString());
				lastMoveTime = newtime;
			}

		}

		//check if it is in the server mode
		public static bool IsServer()
		{
			if (Program.Server != null) return true;
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
				Program.Client.Stop();
			}
			if (Program.Server != null)
			{
				Program.Server.BroadCast((new MSGQuit("ServerHasToQuit\t"+GetUserName())).ToString()); //server notify everybody else
				if (Program.Server.ServerComm != null) Program.Server.Stop();
				if (Program.Client != null) Program.Client.Stop();
			}
			
		}

		/// <summary>
		/// Return a string of information of how many players online and those users who are close
		/// </summary>
		public string GetOnlineUsersInfo()
		{
			string info = "" + OnlineTrains.Players.Count + (OnlineTrains.Players.Count <= 1 ? " Other Player Online" : " Other Players Online");
			TrainCar mine = Program.Simulator.PlayerLocomotive;
			SortedList<double, string> users = new SortedList<double,string>();
			foreach (OnlinePlayer p in OnlineTrains.Players.Values)
			{
				if (p.Train == null) continue;
				var d = WorldLocation.GetDistanceSquared(p.Train.FirstCar.WorldPosition.WorldLocation, mine.WorldPosition.WorldLocation);
				users.Add(Math.Sqrt(d), p.Username);
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

		public static void RemovePlayer(OnlinePlayer p)
		{
			if (Program.Server == null) return; //client will do it by decoding message

			string username = p.Username;
			OnlineTrains.Players.Remove(p.Username);
			Program.Simulator.Trains.Remove(p.Train);
			if (p.Train.Cars.Count > 0 && p.Train.Cars[0].Train == p.Train)
				foreach (TrainCar car in p.Train.Cars)
					car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;

			BroadCast((new MSGQuit(username)).ToString());
		}


		public Train FindPlayerTrain(string user)
		{
			return OnlineTrains.findTrain(user);
		}

		public bool FindPlayerTrain(Train t)
		{
			return OnlineTrains.findTrain(t);
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
			if (UserInput.IsPressed(UserCommands.ControlHorn))	MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HORN", EventID.HornOn)).ToString());

			if (UserInput.IsReleased(UserCommands.ControlHorn)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HORN", EventID.HornOff)).ToString());
			
			if (UserInput.IsPressed(UserCommands.ControlPantographSecond)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO2", (++PantoSecondCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlPantographFirst)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO1", (++PantoFirstCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlBell)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "BELL", (++BellCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlWiper)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "WIPER", (++WiperCount) % 2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlHeadlightIncrease))
			{
				HeadLightCount++; if (HeadLightCount >= 3) HeadLightCount = 2;
				MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}

			if (UserInput.IsPressed(UserCommands.ControlHeadlightDecrease))
			{
				HeadLightCount--; if (HeadLightCount < 0) HeadLightCount = 0;
				MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}
		}

	}
}
