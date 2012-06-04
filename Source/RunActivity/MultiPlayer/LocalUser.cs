using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;
namespace ORTS.MultiPlayer
{
	class LocalUser
	{
		static double lastMoveTime = 0.0f;
		static double lastSwitchTime = 0.0f;
		public static bool Stopped = false;
		public static void Update(double newtime)
		{
			if (Program.Server != null && newtime - lastMoveTime >= 1000f)
			{

				Traveller t = Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller;
				MultiPlayer.MSGMove move = new MultiPlayer.MSGMove();
				move.AddNewItem(MultiPlayer.LocalUser.GetUserName(), Program.Simulator.PlayerLocomotive.Train.SpeedMpS,
					Program.Simulator.PlayerLocomotive.Train.travelled, Program.Simulator.PlayerLocomotive.Train.Number);
				Program.Server.BroadCast(Program.Simulator.OnlineTrains.MoveTrains(move));
				lastMoveTime = newtime;
			}
			if (Program.Server != null && newtime - lastSwitchTime >= 5000f)
			{
				lastSwitchTime = newtime;
				MultiPlayer.LocalUser.BroadCast((new MultiPlayer.MSGSwitchStatus()).ToString());
			}
			if (Program.Client != null && Program.Server == null && newtime - lastMoveTime >= 1000f)
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

	}
}
