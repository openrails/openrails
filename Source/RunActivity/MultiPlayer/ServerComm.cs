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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ORTS.MultiPlayer
{
	public class ServerComm
	{
		private TcpListener tcpListener;
		private Thread listenThread;
		private Server Server;

		public void Stop()
		{
			tcpListener.Stop();
			listenThread.Abort();
			foreach (OnlinePlayer p in Server.Players)
			{
				p.thread.Abort();
			}
		}
		public ServerComm(Server s, int port)
		{
			Server = s;
			this.tcpListener = new TcpListener(IPAddress.Any, port);
			this.listenThread = new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
		}

		private void ListenForClients()
		{
			this.tcpListener.Start();

			while (true)
			{
				TcpClient client = null;
				try
				{
					//blocks until a client has connected to the server
					client = this.tcpListener.AcceptTcpClient();
				}
				catch (Exception) { break; }
				OnlinePlayer player = new OnlinePlayer(client, Server);
				Server.Players.Add(player);
				System.Console.WriteLine("New Player Joined");
				//create a thread to handle communication
				//with connected client
				Thread clientThread = new Thread(new ParameterizedThreadStart(player.Receive));
				player.thread = clientThread;
				clientThread.Start(client);
			}
		}
	}
}
