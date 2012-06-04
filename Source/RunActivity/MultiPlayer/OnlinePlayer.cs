using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;


namespace ORTS.MultiPlayer
{
	public class OnlinePlayer
	{
		public Decoder decoder;
		public OnlinePlayer(TcpClient t, Server s) { Client = t; Server = s; decoder = new Decoder(); }
		public TcpClient Client;
		public Server Server;
		public string Username = null;
		public Train Train;
		public string con;
		public string path; //pat and consist files
		public Thread thread;
		public void Send(string msg)
		{
			NetworkStream clientStream = Client.GetStream();

			try
			{
				byte[] buffer = Encoding.Unicode.GetBytes(msg);//encoder.GetBytes(msg);

				clientStream.Write(buffer, 0, buffer.Length);
				clientStream.Flush();
			}
			catch
			{
			}
		}

		public void Receive(object client)
		{
			NetworkStream clientStream = Client.GetStream();

			byte[] message = new byte[8192];
			int bytesRead;

			while (!LocalUser.Stopped)
			{
				//System.Threading.Thread.Sleep(Program.Random.Next(50, 200));

				bytesRead = 0;

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
						Message msg = Message.Decode(info);
						/*if (Train == null)
						{
							MSGPlayer h = (MSGPlayer)msg;
							Username = string.Copy(h.user);
							this.con = Program.Simulator.BasePath + "\\TRAINS\\CONSISTS\\" + h.con;
							this.path = Program.Simulator.RoutePath + "\\PATHS\\" + h.path;

							Program.Simulator.OnlineTrains.AddPlayers(h, this);
							MSGPlayer host = new MSGPlayer(Program.Server.UserName, "1234", Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.Trains[0],
								Program.Simulator.Trains[0].Number);

							Program.Server.BroadCast(host.ToString()+Program.Simulator.OnlineTrains.AddAllPlayerTrain());
							MSGSwitchStatus msg2 = new MSGSwitchStatus();
							LocalUser.BroadCast(msg2.ToString());

							foreach (Train t in Program.Simulator.Trains)
							{
								if (t == Program.Simulator.Trains[0]) continue; //avoid broadcast player train
								if (t == Train) continue; //avoid broadcast other player's train
								if (Program.Simulator.OnlineTrains.findTrain(t)) continue;
								LocalUser.BroadCast((new MSGTrain(t, t.Number)).ToString());
							}


							//System.Console.WriteLine(host.ToString() + Program.Simulator.OnlineTrains.AddAllPlayerTrain());
						}
						else*/ 
						msg.HandleMsg();

						info = decoder.GetMsg();
					}
				}
				catch (MultiPlayerError)
				{
					break;
				}
				catch (Exception e)
				{
					System.Console.WriteLine(e.Message+info);
				}
			}

			Client.Close();
			Server.Players.Remove(this);
			thread.Abort();
		}

	}
}
