using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using TomNet.Sockets;

namespace client
{
    class Client
    {
        public static TcpSocket dora = new TcpSocket();
        static void Main(string[] args)
        {
            Console.WriteLine("[Client]");

            try
            {
                dora.Connect("127.0.0.1", 9999);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);

            }
            Console.ReadLine();
        }
    }
}
