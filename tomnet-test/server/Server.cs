using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using TomNet.Common;
using System.Threading;

namespace server
{
    class Server
    {
        static public ByteBuffer buf = new ByteBuffer();
        static public int buflen = 32;
        static public int Packlen = 100;
        static public AutoResetEvent autoEvent = new AutoResetEvent(false);
        static public Socket client = null;
        static public Timer stateTimer = null;
        static public bool stop = false;
        static public bool buildpacket = false;
        static public bool initpacket = false;

        static string GenerateChar()
        {
            Random random = new Random();
            return Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65))).ToString();
        }

        static string GenerateChar(int count)
        {
            string randomString = "";

            for (int i = 0; i < count; i++)
            {
                randomString += GenerateChar();
            }
            return randomString;
        }

        static int  BuildPacket(int packetnum)
        {
            if(initpacket)
            {
                buildpacket = true;
                buf.Rollback(0);
                buildpacket = false;
                int len = buf.Readable();
                return len;
            }
            else
            {
                buildpacket = true;
                int totallen = 0;
                for(int j = 0; j < packetnum; j++)
                {
                    Random msgrandom = new Random();
                    int msglen = msgrandom.Next(4, 10*1024);
                    if((msglen % sizeof(int)) != 0)
                    {
                        msglen = msglen * sizeof(int);
                    }

                    buf.AppendInt32(msglen); // 长度

                    string msg = GenerateChar(msglen);
                    byte[] msgbyte = System.Text.Encoding.Default.GetBytes(msg);
                    buf.Write(msgbyte, 0 , msg.Length);
                    totallen += msglen + sizeof(int);
                }
                buildpacket = false;
                initpacket = true;
                return totallen;
            }
        }

        static void TimerInvokes(Object stateInfo)
        {
            if(stop)
            {
                Console.WriteLine("Stop Timer!!!!!");
                stateTimer.Dispose();
                return;
            }

            if(buf.Readable() > 0)
            {
                if(buf.Readable() <= 200)
                {
                    SendData(buf.Readable());
                }
                else
                {
                    Random random = new Random();
                    int len = random.Next(1, buf.Readable());
                    SendData(len);
                }
            }
            else
            {
                BuildPacket(Packlen);
            }
        }

        static void HandSend()
        {
            Console.WriteLine("输入本次要发送的数据长度====>");
            string input = Console.ReadLine();
            int sendlen = 1;
            if (input.IsNormalized())
            {
                sendlen = int.Parse(input);
            }

            while (!(input.ToLower() == "q"))
            {
                SendData(sendlen);
                Console.WriteLine("输入本次要发送的数据长度====>");
                input = Console.ReadLine();
                if (input.IsNormalized())
                {
                    sendlen = int.Parse(input);
                }
            }

        }

        /*
         * 自动发包模式
         * 一次构造Packlen个数据包,每个包的长度随机
         * Packlen个数据包按顺序存放在一个Buffer中
         * 每次从Buffer中截取一个随机长度的字节流片段发出去
         * 接受方收到的数据或属于一个数据包 或属于n个数据包,
         * 无论怎样都要能正确的从流中拆解出完整的数据包
         * 数据包格式 len[4字节] + paylod[随机长度] ,len的值只包含payload的长度
         * * */

        static void AutoSend()
        {
            while(true)
            {
                TimerInvokes(null);
                Thread.Sleep(2);
            }

            //stateTimer = new Timer(TimerInvokes, autoEvent, 0, 5);

        }

        static void SendData(int sendlen)
        {
            byte[] data = buf.ReadBytes(sendlen);
            if (data != null)
            {
                int rawsendlen = client.Send(data);
                if(rawsendlen != sendlen)
                {
                    Console.WriteLine("Send Error");
                    stop = true;
                    return;
                }
                Console.WriteLine("Send len : " + sendlen);

            }
        }


        static void Main(string[] args)
        {
            Console.WriteLine("[Server]");
            try
            {
                Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999));
                socketServer.Listen(int.MaxValue);
                Console.WriteLine("服务端已启动， 127.0.0.1:9999 等待连接...");

                //接收连接
                client = socketServer.Accept();
                client.NoDelay = true;
                Console.WriteLine("客户端已连接...");
                Console.WriteLine("开始构建测试数据包...");
                BuildPacket(Packlen);
                //HandSend();
                AutoSend();
                Console.ReadKey();


            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("按任意键退出");
                Console.ReadKey();
            }

        }
    }
}
