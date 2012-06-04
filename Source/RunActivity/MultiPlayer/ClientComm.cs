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
	public class ClientComm
	{
		private Thread listenThread;
		private TcpClient client;
		public string UserName;
		public string Code;
		public Decoder decoder;

		public void Stop()
		{
			listenThread.Abort();
		}
		public ClientComm(string serverIP, int serverPort, string s)
		{
			client = new TcpClient();

			IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

			client.Connect(serverEndPoint);

			listenThread = new Thread(new ParameterizedThreadStart(this.Receive));
			listenThread.Start(client);

			string[] tmp = s.Split(' ');
			UserName = tmp[0];
			Code = tmp[1];
			decoder = new Decoder();
		}

		public void Receive(object client)
		{

			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();

			byte[] message = new byte[8192];
			int bytesRead;

			while (!LocalUser.Stopped)
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
						Message msg = Message.Decode(info);
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
					System.Console.WriteLine(e.Message + info);
				}
			}

			tcpClient.Close();
			listenThread.Abort();
		}

		public void Send(string msg)
		{
			byte[] buffer = Encoding.Unicode.GetBytes(msg);//encoder.GetBytes(msg);

			try
			{
				NetworkStream clientStream = client.GetStream();
				clientStream.Write(buffer, 0, buffer.Length);
				clientStream.Flush();
			}
			catch
			{
			}
		}

	}
}
