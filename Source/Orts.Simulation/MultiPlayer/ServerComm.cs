// COPYRIGHT 2012 by the Open Rails project.
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Orts.MultiPlayer
{
    public class ServerComm
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private Server Server;
        private int count = 0;
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
            this.listenThread.Name = "Multiplayer Server";
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
                count++;
                OnlinePlayer player = new OnlinePlayer(client, Server);
                Server.Players.Add(player);
                System.Console.WriteLine("New Player Joined");

                //create a thread to handle communication
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(player.Receive));
                clientThread.Name = "Multiplayer Server-Client";// +count;
                player.thread = clientThread;
                clientThread.Start(client);
            }
        }
    }
}
