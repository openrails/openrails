using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;


namespace ORTS
{
    public class NetworkHandler: IDisposable
    {
        public const int PortNumber = 2055;

        static TcpListener listener;
        const int LIMIT = 5; //5 concurrent clients
        Simulator Simulator;

        Thread[] serverThreads = new Thread[LIMIT];

        public NetworkHandler(Simulator simulator)
        {
            Simulator = simulator;

            listener = new TcpListener( IPAddress.Any, PortNumber);
            listener.Start();
            Console.WriteLine("Server mounted, listening to port {0}", PortNumber);

            for (int i = 0; i < LIMIT; i++)
            {
                Thread t = new Thread(new ThreadStart(Server));
                t.Start();
                serverThreads[i] = t;
            }
        }

        public void Server()
        {
            while (true)
            {
                if (listener.Pending() )
                {
                    Socket soc = listener.AcceptSocket();
                    //soc.SetSocketOption(SocketOptionLevel.Socket,
                    //        SocketOptionName.ReceiveTimeout,10000);
                    Console.WriteLine("Connected: {0}",
                                             soc.RemoteEndPoint);
                    try
                    {
                        Stream s = new NetworkStream(soc);
                        StreamReader sr = new StreamReader(s);
                        StreamWriter sw = new StreamWriter(s);
                        sw.AutoFlush = true; // enable automatic flushing
                        sw.WriteLine("Activity = \"{0}\"", Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name);
                        while (true)
                        {
                            string name = sr.ReadLine();
                            sw.WriteLine(name);
                        }
                        s.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Console.WriteLine("Disconnected: {0}",
                                            soc.RemoteEndPoint);

                    soc.Close();
                }

                Thread.Sleep(100);
            }
        }

        public static void Join( Simulator simulator, string basePath, string destinationIP )
        {
            TcpClient client = new TcpClient( IPAddress.Loopback.ToString(), PortNumber);
            try
            {
                Stream s = client.GetStream();
                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);
                sw.AutoFlush = true;
                Console.WriteLine(sr.ReadLine());
                while (true)
                {
                    Console.Write("Name: ");
                    string name = Console.ReadLine();
                    sw.WriteLine(name);
                    if (name == "") break;
                    Console.WriteLine(sr.ReadLine());
                }
                s.Close();
            }
            finally
            {
                // code in finally block is guranteed 
                // to execute irrespective of 
                // whether any exception occurs or does 
                // not occur in the try block
                client.Close();
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            for (int i = 0; i < LIMIT; ++i)
            {
                if( serverThreads != null )
                    serverThreads[i].Abort();
            }
        }

        #endregion
    }// class Network Handler
}
