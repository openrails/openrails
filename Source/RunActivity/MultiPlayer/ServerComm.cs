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
		public ServerComm(Server s)
		{
			Server = s;
			this.tcpListener = new TcpListener(IPAddress.Any, 30000);
			this.listenThread = new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
		}

		private void ListenForClients()
		{
			this.tcpListener.Start();

			while (true)
			{
				//blocks until a client has connected to the server
				TcpClient client = this.tcpListener.AcceptTcpClient();

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
