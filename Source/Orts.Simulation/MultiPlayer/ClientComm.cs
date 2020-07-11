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

// #define DEBUG_MULTIPLAYER
// DEBUG flag for debug prints

using Orts.Simulation.Physics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Orts.MultiPlayer
{
    public class ClientComm
	{
		private Thread listenThread;
		private TcpClient client;
		public string UserName;
		public string Code;
		public Decoder decoder;
		public bool Connected = false;

		public void Stop()
		{
			try
			{
				client.Close();
				listenThread.Abort();
			}
			catch (Exception) { }
		}
		public ClientComm(string serverIP, int serverPort, string s)
		{
			client = new TcpClient();

            IPAddress address;

            if (!IPAddress.TryParse(serverIP, out address))
            {
                address = Dns.GetHostEntry(serverIP)
                     .AddressList
                     .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            }
			IPEndPoint serverEndPoint = new IPEndPoint(address, serverPort);
#if DEBUG_MULTIPLAYER
            Trace.TraceInformation("ClientComm data: {0} , ServerIP: {1}", s, serverIP);
#endif

            client.Connect(serverEndPoint);
			string[] tmp = s.Split(' ');
			UserName = tmp[0];
			Code = tmp[1];
			decoder = new Decoder();

			listenThread = new Thread(new ParameterizedThreadStart(this.Receive));
			listenThread.Name = "Multiplayer Client-Server";
			listenThread.Start(client);

		}

		public void Receive(object client)
		{

			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();

			byte[] message = new byte[8192];
			int bytesRead;

			while (true)
			{
				bytesRead = 0;
				//System.Threading.Thread.Sleep(Program.Random.Next(50, 200));
				try
				{
					//blocks until a client sends a message
					bytesRead = clientStream.Read(message, 0, 8192);
				}
				catch
				{
					//a socket error has occured
					break;
				}

				if (bytesRead == 0)
				{
					//the client has disconnected from the server
					break;
				}

				//message has successfully been received
				string info = "";
				try
				{
					decoder.PushMsg(Encoding.Unicode.GetString(message, 0, bytesRead));//encoder.GetString(message, 0, bytesRead));
					info = decoder.GetMsg();
					while (info != null)
					{
						//System.Console.WriteLine(info);
						Message msg = Message.Decode(info);
						if (Connected || msg is MSGRequired) msg.HandleMsg();
						info = decoder.GetMsg();
					}
				}
				catch (MultiPlayerError)
				{
					break;
				}
				catch (SameNameError) //I have conflict with some one in the game, will close, and abort.
				{
					if (MPManager.Simulator.Confirmer != null)
                        MPManager.Simulator.Confirmer.Error(MPManager.Catalog.GetString("Connection to the server is lost, will play as single mode"));
                    MPManager.Client = null;
					tcpClient.Close();
					listenThread.Abort();
				}
				catch (Exception e)
				{
					System.Console.WriteLine(e.Message + e.StackTrace);
					Trace.TraceWarning(e.Message + e.StackTrace);
				}
			}
			if (MPManager.Simulator.Confirmer != null)
                MPManager.Simulator.Confirmer.Error(MPManager.Catalog.GetString("Connection to the server is lost, will play as single mode"));
			try
			{
				foreach (var p in MPManager.OnlineTrains.Players)
				{
					MPManager.Instance().AddRemovedPlayer(p.Value);
				}
			}
			catch (Exception) { }
			
			//no matter what, let player gain back the control of the player train
			if (MPManager.Simulator.PlayerLocomotive != null && MPManager.Simulator.PlayerLocomotive.Train != null)
			{
				MPManager.Simulator.PlayerLocomotive.Train.TrainType = Train.TRAINTYPE.PLAYER;
				MPManager.Simulator.PlayerLocomotive.Train.LeadLocomotive = MPManager.Simulator.PlayerLocomotive;
			}
			if (MPManager.Simulator.Confirmer != null)
                MPManager.Simulator.Confirmer.Information(MPManager.Catalog.GetString("Alt-E to gain control of your train"));

            MPManager.Client = null;
			tcpClient.Close();
			listenThread.Abort();
		}

		private object lockObj = new object();
		public void Send(string msg)
		{

			try
			{
				NetworkStream clientStream = client.GetStream();
				lock (lockObj)//in case two threads want to write at the same buffer
				{
#if DEBUG_MULTIPLAYER
                    Trace.TraceInformation("MPClientSend: {0}", msg);
#endif
                    byte[] buffer = Encoding.Unicode.GetBytes(msg);//encoder.GetBytes(msg);
					clientStream.Write(buffer, 0, buffer.Length);
					clientStream.Flush();
				}
			}
			catch
			{
			}
		}

	}
}
