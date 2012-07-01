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
		public OnlinePlayer(TcpClient t, Server s) { Client = t; Server = s; decoder = new Decoder(); CreatedTime = Program.Simulator.GameTime; }
		public TcpClient Client;
		public Server Server;
		public string Username = "";
		public Train Train;
		public string con;
		public string path; //pat and consist files
		public Thread thread;
		public double CreatedTime; 

		public void Send(string msg)
		{
			try
			{
				NetworkStream clientStream = Client.GetStream();
				byte[] buffer = Encoding.Unicode.GetBytes(msg);//encoder.GetBytes(msg);

				lock (buffer)//lock the buffer in case two threads want to write at once
				{
					clientStream.Write(buffer, 0, buffer.Length);
					clientStream.Flush();
				}
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
			int errorCount = 0;
			double firstErrorTick = 0;
			double nowTicks = 0;

			while (true)
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
						//System.Console.WriteLine(info);
						Message msg = Message.Decode(info);
						if (msg is MSGPlayer) ((MSGPlayer)msg).HandleMsg(this);
						else msg.HandleMsg();

						info = decoder.GetMsg();
					}
				}
				catch (MultiPlayerError)
				{
					break;
				}
				catch (Exception e)
				{
					nowTicks = Program.Simulator.GameTime;
					if (firstErrorTick == 0)
					{
						firstErrorTick = nowTicks;
						errorCount = 1;
					}
					if (errorCount >= 10 && nowTicks - firstErrorTick < 100000000) //10 errors last 10 seconds
					{
						MSGMessage emsg = new MSGMessage(this.Username, "Error", "Too many errors received from you in a short period of time.");
						MPManager.BroadCast(emsg.ToString());
						break;
					}
					else if (errorCount < 10) { errorCount++; }
					else { firstErrorTick = nowTicks; errorCount = 0; }
					//System.Console.WriteLine(e.Message + info);
				}
			}

			System.Console.WriteLine(this.Username + "quit");
			if (Program.Simulator.Confirmer != null) Program.Simulator.Confirmer.Message("Info:", this.Username + " quit.");
			Client.Close();
			MPManager.Instance().AddRemovedPlayer(this);//add this player to be removed
			MPManager.BroadCast((new MSGQuit(this.Username)).ToString());
			thread.Abort();
		}

	}
}
