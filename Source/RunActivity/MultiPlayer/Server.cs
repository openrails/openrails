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

namespace ORTS.MultiPlayer
{
	public class Server
	{
		public List<OnlinePlayer> Players;
		public string UserName;
		public string Code;
		ClientComm Connection;
		public ServerComm ServerComm;
		public int ConnectionMode;

		public void Stop()
		{
			if (ServerComm != null) ServerComm.Stop();
			if (Connection != null) Connection.Stop();
		}
		public Server(string s, ClientComm c)
		{
			Players = new List<OnlinePlayer>();
			string[] tmp = s.Split(' ');
			UserName = tmp[0];
			Code = tmp[1];
			Connection = c;
			ServerComm = null;
			ConnectionMode = 0;
		}

		public bool IsRemoteServer()
		{
			if (ConnectionMode == 0) return true;
			else return false;
		}
		public Server(string s, int port)
		{
			Players = new List<OnlinePlayer>();
			string[] tmp = s.Split(' ');
			UserName = tmp[0];
			Code = tmp[1];

			ServerComm = new ServerComm(this, port);
			Connection = null;
			ConnectionMode = 1;
		}
		
		public void BroadCast(string msg)
		{
			if (ServerComm == null) Connection.Send(msg);
			else
			{
				try
				{
					foreach (OnlinePlayer p in Players)
					{
						p.Send(msg);
					}
				}
				catch (Exception) { }
			}
		}
	}
}
