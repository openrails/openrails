using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;
namespace ORTS.MultiPlayer
{
	//a singleton class handles communication, update and stop etc.
	class LocalUser
	{
		double lastMoveTime = 0.0f;
		double lastSwitchTime = 0.0f;
		string[] info;
		int maxLinesOfInfo = 4;
		private static LocalUser localUser = null;

		private LocalUser()
		{
			info = new string[maxLinesOfInfo]; //maxLinesOfInfo lines of information
		}
		public static LocalUser Instance()
		{
			if (localUser == null) localUser = new LocalUser();
			return localUser;
		}

		public void Update(double newtime)
		{
			handleUserInput();
			//server update train location of all
			if (Program.Server != null && newtime - lastMoveTime >= 1f)
			{

				Traveller t = Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller;
				MultiPlayer.MSGMove move = new MultiPlayer.MSGMove();
				move.AddNewItem(MultiPlayer.LocalUser.GetUserName(), Program.Simulator.PlayerLocomotive.Train.SpeedMpS,
					Program.Simulator.PlayerLocomotive.Train.travelled, Program.Simulator.PlayerLocomotive.Train.Number);
				Program.Server.BroadCast(Program.Simulator.OnlineTrains.MoveTrains(move));
				lastMoveTime = newtime;
			}
			//server updates switch
			if (Program.Server != null && newtime - lastSwitchTime >= 5f)
			{
				lastSwitchTime = newtime;
				MultiPlayer.LocalUser.BroadCast((new MultiPlayer.MSGSwitchStatus()).ToString());
			}
			//client updates itself
			if (Program.Client != null && Program.Server == null && newtime - lastMoveTime >= 1f)
			{
				Traveller t = Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller;
				MultiPlayer.MSGMove move = new MultiPlayer.MSGMove();
				move.AddNewItem(MultiPlayer.LocalUser.GetUserName(), Program.Simulator.PlayerLocomotive.Train.SpeedMpS,
					Program.Simulator.PlayerLocomotive.Train.travelled, Program.Simulator.PlayerLocomotive.Train.Number);
				Program.Client.Send(move.ToString());
				lastMoveTime = newtime;
			}

		}
		public static bool IsServer()
		{
			if (Program.Server != null) return true;
			else return false;
		}
		static public string GetUserName()
		{
			if (Program.Server != null) return Program.Server.UserName;
			else if (Program.Client != null) return Program.Client.UserName;
			else return "";
		}

		static public bool IsMultiPlayer()
		{
			if (Program.Server != null || Program.Client != null) return true;
			else return false;
		}

		static public void BroadCast(string m)
		{
			if (Program.Server != null) Program.Server.BroadCast(m);
		}

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

		static public void Stop()
		{
			if (Program.Client != null && Program.Server == null)
			{
				Program.Client.Send((new MSGQuit(GetUserName())).ToString()); //client notify server
				Program.Client.Stop();
			}
			if (Program.Server != null)
			{
				Program.Server.BroadCast((new MSGQuit("ServerHasToQuit")).ToString()); //server notify everybody else
				if (Program.Server.ServerComm != null) Program.Server.Stop();
			}
			
		}

		public string[] GetOnlineUsers(ref int count)
		{
			info[0] = "" + Program.Simulator.OnlineTrains.Players.Count + (Program.Simulator.OnlineTrains.Players.Count <= 1 ? " Other Player Online" : " Other Players Online");
			TrainCar mine = Program.Simulator.PlayerLocomotive;
			var i = 1;
			foreach (OnlinePlayer p in Program.Simulator.OnlineTrains.Players.Values)
			{
				var d = WorldLocation.GetDistanceSquared(p.Train.FirstCar.WorldPosition.WorldLocation, mine.WorldPosition.WorldLocation);
				info[i++] = p.Username + ": distance " + (int)Math.Sqrt(d);
				if (i >= maxLinesOfInfo) break;
			}
			count = i;
			return info;
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
			if (UserInput.IsPressed(UserCommands.ControlHorn))	LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "HORN", EventID.HornOn)).ToString());

			if (UserInput.IsReleased(UserCommands.ControlHorn)) LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "HORN", EventID.HornOff)).ToString());
			
			if (UserInput.IsPressed(UserCommands.ControlPantographSecond)) LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "PANTO2", (++PantoSecondCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlPantographFirst)) LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "PANTO1", (++PantoFirstCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlBell)) LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "BELL", (++BellCount)%2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlWiper)) LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "WIPER", (++WiperCount) % 2)).ToString());

			if (UserInput.IsPressed(UserCommands.ControlHeadlightIncrease))
			{
				HeadLightCount++; if (HeadLightCount >= 3) HeadLightCount = 2;
				LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}

			if (UserInput.IsPressed(UserCommands.ControlHeadlightDecrease))
			{
				HeadLightCount--; if (HeadLightCount < 0) HeadLightCount = 0;
				LocalUser.Notify((new MSGEvent(LocalUser.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
			}

		}

	}
}
