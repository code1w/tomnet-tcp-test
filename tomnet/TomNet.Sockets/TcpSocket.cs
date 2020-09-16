
using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using TomNet.Common;

namespace TomNet.Sockets
{
    public class TcpSocket : BaseSocket
    {
        private static readonly int  TcpBufferSize = 8192;
        private static int connid = 0;
        private int socketPollSleep;
        private Thread connthread;
        private string host;
        private int port;
        private TcpClient conn;
        private NetworkStream connstream;
        private Thread readthread;
        private byte[] buffer = new byte[4096];
        private ByteBuffer recvbuf_;

        States currentstates = States.Disconnected;

        public bool IsConnected  = false;
      


        public TcpSocket()
        {
            recvbuf_ = new ByteBuffer();
        }

        private void LogWarn(string msg)
        {
        }

        private void LogError(string msg)
        {
        }

        /// <summary>
        /// 启动线程负责建立网络连接
        /// </summary>
        private void ConnectThread()
        {
            Thread.CurrentThread.Name = "ConnectionThread" + connid++;
            try
            {
                currentstates = States.Connecting;
                conn = new TcpClient(host, port);
                connstream = conn.GetStream();
                ConfigureTcpSocket(conn.Client);
                currentstates = States.Connected;
                CallOnConnect();
                readthread = new Thread(Read);
                readthread.IsBackground = true;
                readthread.Start();
            }
            catch (SocketException ex)
            {
                string err = "Connection error: " + ex.Message + " " + ex.StackTrace;
                HandleError(err, ex.SocketErrorCode);
            }
            catch (Exception ex2)
            {
                string err2 = "General exception on connection: " + ex2.Message + " " + ex2.StackTrace;
                HandleError(err2);
            }
        }

        private void HandleError(string err)
        {
            HandleError(err, SocketError.NotSocket);
        }

        private void HandleError(string err, SocketError se)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["err"] = err;
            hashtable["se"] = se;
            Console.WriteLine(err);
            Console.WriteLine(se);
        }

        private void HandleErrorCallback(object state)
        {
            Hashtable hashtable = state as Hashtable;
            string msg = (string)hashtable["err"];
            SocketError se = (SocketError)hashtable["se"];
            
            if (!isDisconnecting)
            {
                LogError(msg);
                CallOnError(msg, se);
            }
            HandleDisconnection();
        }

        private void HandleDisconnection()
        {
            HandleDisconnection(null);
        }

        private void HandleDisconnection(string reason)
        {
            if (currentstates == States.Connected)
            {
                currentstates = States.Disconnected;
                CallOnDisconnect(reason);
            }
        }

        private void WriteSocket(byte[] buf)
        {
            if (currentstates != States.Connected)
            {
                LogError("Trying to write to disconnected socket");
                return;
            }
            try
            {
                connstream.Write(buf, 0, buf.Length);
            }
            catch (SocketException ex)
            {
                string err = "Error writing to socket: " + ex.Message;
                HandleError(err, ex.SocketErrorCode);
            }
            catch (Exception ex2)
            {
                string err2 = "General error writing to socket: " + ex2.Message + " " + ex2.StackTrace;
                HandleError(err2);
            }
        }

        private static void Sleep(int ms)
        {
            Thread.Sleep(10);
        }

        private void Read()
        {
            int num = 0;
            while (true)
            {
                try
                {
                    if (currentstates != States.Connected)
                    {
                        return;
                    }
                    if (socketPollSleep > 0)
                    {
                        Sleep(socketPollSleep);
                    }
                    num = connstream.Read(buffer, 0, buffer.Length);
                    if (num < 1)
                    {
                        HandleError("Connection closed by the remote side");
                        return;
                    }
                    HandleBinaryData(buffer, num);
                }
                catch (Exception ex)
                {
                    HandleError("General error reading data from socket: " + ex.Message + " " + ex.StackTrace);
                    return;
                }
            }
        }

        private void HandleBinaryData(byte[] buf, int size)
        {
            recvbuf_.Write(buf, 0, size);
            CallOnData();
        }

        public void Connect(string host, int port)
        {
            if (currentstates != 0)
            {
                LogWarn("Call to Connect method ignored, as the socket is already connected");
                return;
            }
            this.host = host;
            this.port = port;
            connthread = new Thread(ConnectThread);
            connthread.Start();
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public void Disconnect(string reason)
        {
            if (currentstates != States.Connected)
            {
                LogWarn("Calling disconnect when the socket is not connected");
                return;
            }
            isDisconnecting = true;
            try
            {
                conn.Client.Shutdown(SocketShutdown.Both);
                conn.Close();
                connstream.Close();
            }
            catch (Exception)
            {
            }
            HandleDisconnection(reason);
            isDisconnecting = false;
        }

        public void Kill()
        {
            currentstates = States.Disconnected;
            conn.Close();
        }

        private void CallOnData(byte[] data)
        {
        }

        private void CallOnData()
        {

            while(recvbuf_.Readable() >= sizeof(int))
            {
                int packlen = recvbuf_.PeekInt32(); 
                
                if(recvbuf_.Readable() - sizeof(int) < packlen)
                {
                    break; 
                }

                ByteBuffer data = new ByteBuffer();
                packlen = recvbuf_.ReadInt32();
                data.AppendInt32(packlen);
                data.Write(recvbuf_.Begin(), recvbuf_.PrependableBytes(), packlen);
                recvbuf_.Skip(packlen);

                DebugPacketData(ref data);
            }
        }

        private void CallOnError(string msg, SocketError se)
        {
        }

        private void CallOnConnect()
        {
        }

        private void CallOnDisconnect(string reason)
        {
        }

        public void Write(byte[] data)
        {
            WriteSocket(data);
        }

        private void DebugPacketData(ref ByteBuffer data)
        {
            int len = data.ReadInt32();
            byte[] dbytes = new byte[len];
            Buffer.BlockCopy(data.Begin(), data.PrependableBytes(), dbytes, 0, data.Readable());
            string str = System.Text.Encoding.Default.GetString(dbytes);
            Console.WriteLine($"packet len : " + len);
        }

        private void ConfigureTcpSocket(Socket tcpSocket)
        {
            tcpSocket.NoDelay = true;
            tcpSocket.ReceiveBufferSize = TcpBufferSize;
            tcpSocket.SendBufferSize = TcpBufferSize;
        }
    }
}
