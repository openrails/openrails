//
// Code forked from Open Rails Ultimate (now FreeTrainSimulator)
//
using System;
using System.Threading.Tasks;

namespace MultiPlayerServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Open Rails Multiplayer Server";
            try
            {
                int port = 30000;
                if (args.Length > 0 && !int.TryParse(args[0], out port))
                    port = 30000;
                Host server = new Host(port);
                Task serverTask = server.Run();
                if (serverTask.IsFaulted)
                {
                    return;
                }
                else
                {
                    Console.ReadLine();
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
